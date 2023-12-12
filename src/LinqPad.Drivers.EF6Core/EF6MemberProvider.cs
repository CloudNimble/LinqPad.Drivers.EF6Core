using LINQPad;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using Z.EntityFramework.Extensions.Core.Infrastructure;

namespace CloudNimble.LinqPad.Drivers.EF6Core
{

    /// <summary>
    ///     Decompiled from LINQPad.Extensibility.DataContext.EntityFrameworkMemberProvider class of LINQPad 5 by Joseph
    ///     Albahari
    /// </summary>
    internal class EF6MemberProvider : ICustomMemberProvider
    {
        private static MethodInfo getItemMethod;
        private static MethodInfo getEntitySetMethod;

        private static readonly Dictionary<Type, (List<string> Names, List<Type> Types)> typeMappings = [];

        private readonly List<string> names;
        private readonly List<Type> types;
        private readonly List<object> values;





        public EF6MemberProvider(DbContext dbContext, DbModel model, Type objectType, string conceptualTypeName, object objectToWrite)
        {

            if (typeMappings.TryGetValue(objectType, out (List<string> Names, List<Type> Types) value))
            {
                names = value.Names;
                types = value.Types;
                return;
            }

            var navPropNames = model.ConceptualModel.EntityTypes
                .Where(c => c.Name == conceptualTypeName)
                .FirstOrDefault()
                .NavigationProperties
                .Select(c => c.Name);

            var propertyNames = model.ConceptualModel.EntityTypes
                .Where(c => c.Name == conceptualTypeName)
                .FirstOrDefault()
                .Properties
                .Select(c => c.Name);



            var entityFrameworkMemberProvider = this;

            var navProps = new HashSet<string>(GetNavPropNames(dbContext, objectToWrite) ?? Array.Empty<string>());
            var source = (from m in objectToWrite.GetType().GetMembers()
                          where m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property
                          where (m.MemberType != MemberTypes.Field || m.Name != "_entityWrapper") &&
                              (m.MemberType != MemberTypes.Property || m.Name != "RelationshipManager")
                          let isNav = navProps.Contains(m.Name)
                          let type = GetFieldPropType(m)
                          orderby isNav, m.MetadataToken
                          select new
                          {
                              m.Name,
                              type,
                              value = ((isNav && (IsUnloadedEntityAssociation(dbContext, objectToWrite, m) ?? true))
                                  ? Utilities.OnDemand(m.Name, () => entityFrameworkMemberProvider.GetFieldPropValue(objectToWrite, m), runOnNewThread: false, typeof(IEnumerable).IsAssignableFrom(type))
                                  : entityFrameworkMemberProvider.GetFieldPropValue(objectToWrite, m))
                          }).ToList();


            names = source.Select(q => q.Name).ToList();
            types = source.Select(q => q.type).ToList();
            values = source.Select(q => q.value).ToList();
        }

        public IEnumerable<string> GetNames() => names;

        public IEnumerable<Type> GetTypes() => types;

        public IEnumerable<object> GetValues() => values;

        #region Stuff that needs to be rewritten

        private static Type GetFieldPropType(MemberInfo m) =>
            m switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                _ => throw new InvalidOperationException("Expected FieldInfo or PropertyInfo")
            };

        private object GetFieldPropValue(object value, MemberInfo m)
        {
            try
            {
                return m switch
                {
                    FieldInfo fieldInfo => fieldInfo.GetValue(value),
                    PropertyInfo propertyInfo => propertyInfo.GetValue(value, null),
                    _ => throw new InvalidOperationException("Expected FieldInfo or PropertyInfo")
                };
            }
            catch (Exception result)
            {
                return result;
            }
        }

        private IEnumerable<string> GetNavPropNames(object objectContext, object entity)
        {
            if (entity is null || objectContext is null) return null;

            var field = entity.GetType().GetField("_entityWrapper");
            if (field is null) return null;

            var value = field.GetValue(entity);
            var property = value?.GetType().GetProperty("EntityKey");
            if (property is null) return null;

            dynamic value2 = property.GetValue(value, null);
            if (value2 is null || value2.GetType().Name != "EntityKey") return null;

            var val = ((dynamic)objectContext).MetadataWorkspace;
            Type type = val.GetType();

            getItemMethod ??= type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "TryGetItem" && m.IsGenericMethod && m.GetParameters().Length == 3 &&
                                        m.GetParameters()[0].ParameterType == typeof(string) && m.GetParameters()[1].ParameterType.Name == "DataSpace" &&
                                        m.GetParameters()[2].IsOut);
            if (getItemMethod is null) return null;

            var type2 = type.Assembly.GetType("System.Data.Entity.Core.Metadata.Edm.EntityContainer") ??
                type.Assembly.GetType("System.Data.Metadata.Edm.EntityContainer");

            if (type2 is null) return null;
            var methodInfo = getItemMethod.MakeGenericMethod(type2);
            var array = new object[]
            {
            value2.EntityContainerName,
            1,
            null
            };
            if ((!true.Equals(methodInfo.Invoke(val, array)))) return null;
            var obj = array[2];
            if (obj is null) return null;
            if (getEntitySetMethod is null || getEntitySetMethod.ReflectedType != obj.GetType())
                getEntitySetMethod = obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "TryGetEntitySetByName" && m.GetParameters().Length == 3 &&
                                        m.GetParameters()[0].ParameterType == typeof(string) && m.GetParameters()[1].ParameterType == typeof(bool) &&
                                        m.GetParameters()[2].IsOut);
            if (getEntitySetMethod is null) return null;
            array =
            [
                value2.EntitySetName,
            false,
            null
            ];
            if (!true.Equals(getEntitySetMethod.Invoke(obj, array))) return null;
            dynamic val2 = array[2];
            if (val2 is null) return null;
            return ((IEnumerable<object>)val2.ElementType.NavigationProperties).Select((dynamic np) => np.Name).OfType<string>();
        }

        private static bool? IsUnloadedEntityAssociation(dynamic dbContext, dynamic target, MemberInfo member)
        {
            try
            {
                var type = member is FieldInfo fieldInfo ? fieldInfo.FieldType : ((PropertyInfo)member).PropertyType;
                if (type.Name == "ICollection`1" || typeof(ICollection).IsAssignableFrom(type))
                {
                    try
                    {
                        return !dbContext.Entry(target).Collection(member.Name).IsLoaded;
                    }
                    catch
                    {
                        // ignored
                    }

                    return !dbContext.Entry(target).Reference(member.Name).IsLoaded;
                }

                try
                {
                    return !dbContext.Entry(target).Reference(member.Name).IsLoaded;
                }
                catch
                {
                    // ignored
                }

                return !dbContext.Entry(target).Collection(member.Name).IsLoaded;
            }
            catch
            {
                return null;
            }
        }
    }

    #endregion

}
