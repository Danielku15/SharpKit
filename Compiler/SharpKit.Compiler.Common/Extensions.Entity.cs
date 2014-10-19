using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Extensions;

namespace SharpKit.Compiler
{
    public class AssemblyExt
    {
        private List<ResolvedAttribute> _resolvedAttributes;

        public IAssembly Assembly { get; private set; }

        public List<ResolvedAttribute> ResolvedAttributes
        {
            get
            {
                return _resolvedAttributes ??
                       (_resolvedAttributes = Assembly.AssemblyAttributes.Select(ToResolvedAttribute).ToList());
            }
        }

        public AssemblyExt(IAssembly me)
        {
            Assembly = me;
        }

        private ResolvedAttribute ToResolvedAttribute(IAttribute att)
        {
            return new ResolvedAttribute(Assembly) { IAttribute = att };
        }
    }

    public class EntityExt
    {
        private List<ResolvedAttribute> _resolvedAttributes;
        private List<ResolvedAttribute> _externalResolvedAttributes;
        private List<ResolvedAttribute> _parentExternalResolvedAttributes;

        public IEntity Entity { get; private set; }
        public Dictionary<Type, object> SingleDeclaredAttributeCache { get; set; }
        public List<ResolvedAttribute> ResolvedAttributes
        {
            get
            {
                return _resolvedAttributes ??
                       (_resolvedAttributes = Entity.Attributes.Select(ToResolvedAttribute).ToList());
            }
        }

        public List<ResolvedAttribute> ExternalResolvedAttributes
        {
            get
            {
                return _externalResolvedAttributes ?? (_externalResolvedAttributes = new List<ResolvedAttribute>());
            }
        }

        public List<ResolvedAttribute> ParentExternalResolvedAttributes
        {
            get
            {
                if (_parentExternalResolvedAttributes == null)
                {
                    _parentExternalResolvedAttributes = new List<ResolvedAttribute>();
                    var me2 = Entity as IMember;
                    if (me2 != null && me2.MemberDefinition != me2 && me2.MemberDefinition != null)
                    {
                        var me3 = me2.MemberDefinition;
                        var ext3 = me3.GetExtension(false);
                        if (ext3 != null)
                        {
                            if (ext3.ExternalResolvedAttributes != null)
                                _parentExternalResolvedAttributes.AddRange(ext3.ExternalResolvedAttributes);
                        }
                    }
                }
                return _parentExternalResolvedAttributes;
            }
        }

        public IEnumerable<ResolvedAttribute> AllResolvedAttributes
        {
            get
            {
                return ExternalResolvedAttributes.Concat(ParentExternalResolvedAttributes).Concat(ResolvedAttributes);
            }
        }

        public bool? IsExported { get; set; }
        public bool? IsRemotable { get; set; }
        public bool? IsGlobalType { get; set; }
        public bool? IsNativeType { get; set; }

        public EntityExt(IEntity me)
        {
            Entity = me;
        }

        private ResolvedAttribute ToResolvedAttribute(IAttribute att)
        {
            return new ResolvedAttribute(Entity) { IAttribute = att };
        }
    }

    public class ResolvedAttribute
    {
        private string _attributeTypeName;
        private Type _matchedType;
        private HashSet<Type> _unmatchedTypes;

        readonly SkProject _project;

        public IEntity EntityOwner { get; set; }
        public IAssembly AsmOwner { get; set; }
        public IAttribute IAttribute { get; set; }
        public Attribute Attribute { get; set; }

        public ResolvedAttribute(IEntity owner)
        {
            EntityOwner = owner;
            if (EntityOwner != null)
                _project = (SkProject)EntityOwner.GetNProject();
        }

        public ResolvedAttribute(IAssembly owner)
        {
            AsmOwner = owner;
            if (AsmOwner != null)
                _project = (SkProject)AsmOwner.GetNProject();
        }

        public ICSharpCode.NRefactory.CSharp.AstNode GetDeclaration()
        {
            if (IAttribute == null)
                return null;
            return IAttribute.GetDeclaration();
        }

        public bool MatchesType<T>()
        {
            var type = typeof(T);
            if (_matchedType != null)
                return type == _matchedType;
            if (_unmatchedTypes != null && _unmatchedTypes.Contains(type))
                return false;
            var x = IsMatch<T>();
            if (x)
            {
                _matchedType = type;
            }
            else
            {
                if (_unmatchedTypes == null)
                    _unmatchedTypes = new HashSet<Type>();
                _unmatchedTypes.Add(type);
            }
            return x;
        }

        private bool IsMatch<T>()
        {
            if (Attribute != null && Attribute is T)
            {
                return true;
            }
            if (IAttribute != null)
            {
                if (_attributeTypeName == null)
                    _attributeTypeName = IAttribute.AttributeType.FullName;
                var name2 = typeof(T).FullName;
                if (name2.StartsWith(Sk.MirrorTypePrefixExternal, StringComparison.InvariantCultureIgnoreCase))
                    name2 = Sk.MirrorTypePrefixInternal + name2.Substring(Sk.MirrorTypePrefixExternal.Length);
                if (_attributeTypeName == name2)
                {
                    return true;
                }
            }
            return false;
        }

        public T ConvertToCustomAttribute<T>() where T : Attribute
        {
            if (Attribute == null && IAttribute != null)
                Attribute = IAttribute.ConvertToCustomAttribute<T>(_project);
            return Attribute as T;
        }
    }

    public static class EntityExtProvider
    {
        public static IEnumerable<ResolvedAttribute> GetAllResolvedAttributes(this IEntity ent)
        {
            return ent.GetExtension(true).AllResolvedAttributes;
        }

        public static AssemblyExt GetExtension(this IAssembly ent, bool create)
        {
            var ext = (AssemblyExt)ent.Tag;
            if (ext == null && create)
            {
                ext = new AssemblyExt(ent);
                ent.Tag = ext;
            }
            return ext;
        }

        public static EntityExt GetExtension(this IEntity ent, bool create)
        {
            var ext = (EntityExt)ent.Tag;
            if (ext == null && create)
            {
                ext = new EntityExt(ent);
                ent.Tag = ext;
            }
            return ext;
        }
    }
}