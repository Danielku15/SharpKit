﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Extensions;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.Java;

namespace SharpKit.Compiler.Targets.Java
{
    /// <summary>
    /// Finds assembly JsTypeAttribute(s) with TargetType, and adds them to the target type.
    /// Finds assembly JsMethodAttribute(s) with TargetType and TargetMethod, and adds them to the target method(s)
    /// </summary>
    class JExternalMetadata : ICsExternalMetadata
    {
        private readonly List<ITypeDefinition> _typesWithExternalAttributes = new List<ITypeDefinition>();

        public CompilerLogger Log { get; set; }
        public SkProject Project { get; set; }

        public IEnumerable<ITypeDefinition> TypesWithExternalAttributes
        {
            get { return _typesWithExternalAttributes; }
        }

        //public List<IMethod> MethodsWithExternalMetadata = new List<IMethod>();
        //public List<IProperty> PropertiesWithExternalMetadata = new List<IProperty>();
        //public Dictionary<IEntity, List<IAttribute>> ExternalMetadata = new Dictionary<IEntity, List<IAttribute>>();
        ITypeDefinition ResolveType(IType ce, string typeName)
        {
            if (ce != null)
                return ce.GetDefinition();
            if (typeName.IsNullOrEmpty())
                return null;
            var type = Project.FindType(typeName);
            return type;
        }
        public void Process()
        {
            var assemblies = new[] { Project.Compilation.MainAssembly }.Concat(Project.Compilation.GetReferencedAssemblies());
            var list2 = assemblies.Select(t => t.GetExtension(true));
            foreach (var asm in list2)
            {
                if (asm.ResolvedAttributes == null)
                    continue;
                var atts = asm.ResolvedAttributes.FindByType<JTypeAttribute>()
                    .Select(t => new { Entity = t, Att = t.ConvertToCustomAttribute<JTypeAttribute>() }).Where(t => t.Att != null && (t.Att.TargetType != null || t.Att.TargetTypeName.IsNotNullOrEmpty()))
                    .Select(t => new ExternalAttribute { Entity = t.Entity, TargetType = t.Att.TargetType, TargetTypeName = t.Att.TargetTypeName }).ToList();

                var atts2 = asm.ResolvedAttributes.FindByType<JEnumAttribute>().Select(t => new { Entity = t, Att = t.ConvertToCustomAttribute<JEnumAttribute>() }).Where(t => t.Att != null && (t.Att.TargetType != null || t.Att.TargetTypeName.IsNotNullOrEmpty())).Select(t => new ExternalAttribute { Entity = t.Entity, TargetType = t.Att.TargetType, TargetTypeName = t.Att.TargetTypeName }).ToList();
                var atts3 = atts.Concat(atts2);

                foreach (var pair in atts3)
                {
                    var ce = ResolveType(pair.TargetType, pair.TargetTypeName);
                    //TODO: this is also possible, maybe better
                    //ce.Attributes.Add(pair.Entity);

                    _typesWithExternalAttributes.Add(ce);
                    ce.GetExtension(true).ExternalResolvedAttributes.Add(pair.Entity);
                };
                asm.ResolvedAttributes.FindByType<JMethodAttribute>().ForEach(t =>
                {
                    var att = t.ConvertToCustomAttribute<JMethodAttribute>();
                    if (att != null)
                    {
                        var ce = ResolveType(att.TargetType, att.TargetTypeName);
                        if (ce != null)
                        {
                            var methods = ce.GetMethods(att.TargetMethod).ToList();
                            if (methods.Count == 0)
                                Log.Error(t.GetDeclaration(), String.Format("Type {0} does not contain a method named {1}, please modify the TargetMethod property on the specified JsMethodAttribute, if this method is inherited, set the please set the base type as the TargetType.", ce.FullName, att.TargetMethod));
                            methods.ForEach(me =>
                            {
                                //MethodsWithExternalMetadata.Add(me);
                                me.GetExtension(true).ExternalResolvedAttributes.Add(t);
                            });
                        }
                    }
                });
                asm.ResolvedAttributes.FindByType<JPropertyAttribute>().ForEach(t =>
                {
                    var att = t.ConvertToCustomAttribute<JPropertyAttribute>();
                    if (att != null)
                    {
                        var ce = ResolveType(att.TargetType, att.TargetTypeName);
                        if (ce != null)
                        {
                            var pe = ce.GetProperty(att.TargetProperty);
                            if (pe == null)
                                Log.Error(t.IAttribute.GetDeclaration(), String.Format("Type {0} does not contain a property named {1}, please modify the TargetMethod property on the specified JsMethodAttribute, if this method is inherited, set the please set the base type as the TargetType.", ce.FullName, att.TargetProperty));
                            //PropertiesWithExternalMetadata.Add(pe);
                            pe.GetExtension(true).ExternalResolvedAttributes.Add(t);
                        }
                    }
                });
            }
        }
    }
}
