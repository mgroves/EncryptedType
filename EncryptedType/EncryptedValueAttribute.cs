﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

using PostSharp;
using PostSharp.Serialization;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Advices;

namespace EncryptedType
{
    [PSerializable]
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(EncryptedTypeAttribute))]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class EncryptedValueAttribute : LocationInterceptionAspect, IInstanceScopedAspect
    {

        private string propname;

        public override void CompileTimeInitialize(PostSharp.Reflection.LocationInfo targetLocation, AspectInfo aspectInfo)
        {
            propname = targetLocation.Name;
        }

        [ImportMember("KeyCache", IsRequired=true)]
        public Property<IDictionary<string,KeyInfo>> KeyCacheStore;

        [ImportMember("EncryptedValues", IsRequired = true)]
        public Property<IDictionary<string, string>> EncryptedValuesStore;

        [ImportMember("EncryptionKeys", IsRequired = true)]
        public Property<IDictionary<string, string>> EncryptionKeysStore;

        [ImportMember("KeyServer", IsRequired = true)]
        public Property<IKeyServer> KeyServer;

        [ImportMember("Integrity", IsRequired = false)]
        public Property<IDictionary<string,Func<string>>> IntegrityFunction;

        [ImportMember("Encrypt", IsRequired = true, Order=ImportMemberOrder.AfterIntroductions)]
        public Func<string, string, Func<string>, string> Encrypt;

        [ImportMember("Decrypt", IsRequired = true, Order = ImportMemberOrder.AfterIntroductions)]
        public Func<string, string, Func<string>, string> Decrypt;

        public object CreateInstance(AdviceArgs adviceArgs) { return this.MemberwiseClone(); }

        public void RuntimeInitializeInstance() { }

        public override void OnSetValue(LocationInterceptionArgs args)
        {
            if(EncryptionKeysStore.Get().ContainsKey(propname))
            {
                string keyName = EncryptionKeysStore.Get()[propname];
                Func<string> integrity = null;
                if (null != this.IntegrityFunction.Get() && this.IntegrityFunction.Get().ContainsKey(propname))
                    integrity = this.IntegrityFunction.Get()[propname];
                var encrypted = Encrypt(args.Value.ToString(), keyName, integrity);
                if (null != EncryptedValuesStore.Get())
                    if (!EncryptedValuesStore.Get().ContainsKey(propname))
                        EncryptedValuesStore.Get().Add(propname, encrypted);
                    else
                        EncryptedValuesStore.Get()[propname] = encrypted;
            }
        }

        public override void OnGetValue(LocationInterceptionArgs args)
        {
            if (EncryptedValuesStore.Get().ContainsKey(propname))
                args.Value = EncryptedValuesStore.Get()[propname];
        }

    }
}
