using TMS.Models.Account;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TMS.Repository.Extensions
{
    internal static class IQueryableExtension
    {
        public static IQueryable<TModel> OrderBy<TModel>(this IQueryable<TModel> query, List<DataTableColumnsOrder> orderColumns)
        {
            if (orderColumns == null || orderColumns.Count == 0) return query;
            IOrderedQueryable<TModel> orderedQuery = query as IOrderedQueryable<TModel>;
            for (int i = 0; i < orderColumns.Count; i++)
            {
                var column = orderColumns[i];
                if (string.IsNullOrWhiteSpace(column.Column)) continue;
                if (column.Column.Contains(','))
                {
                    var arr = column.Column.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (arr.Length > 1)
                    {
                        if (i == 0)
                        {
                            if (arr[0].Contains('.'))
                            {
                                var navigation1 = arr[0].Split('.');
                                var navigation2 = arr[1].Split('.');
                                if (column.Dir == "desc")
                                    orderedQuery = query.OrderByDescending(t => EF.Property<object>(EF.Property<object>(t!, navigation1[0]), navigation1[1]) ?? EF.Property<object>(EF.Property<object>(t!, navigation2[0]), navigation2[1]));
                                else
                                    orderedQuery = query.OrderBy(t => EF.Property<object>(EF.Property<object>(t!, navigation1[0]), navigation1[1]) ?? EF.Property<object>(EF.Property<object>(t!, navigation2[0]), navigation2[1]));
                            }
                            else
                            {
                                if (column.Dir == "desc")
                                    orderedQuery = query.OrderByDescending(t => EF.Property<object>(t!, arr[0]) ?? EF.Property<object>(t!, arr[1]));
                                else
                                    orderedQuery = query.OrderBy(t => EF.Property<object>(t!, arr[0]) ?? EF.Property<object>(t!, arr[1]));
                            }
                        }
                        else
                        {
                            if (arr[0].Contains('.'))
                            {
                                var navigation1 = arr[0].Split('.');
                                var navigation2 = arr[1].Split('.');
                                if (column.Dir == "desc")
                                    orderedQuery = orderedQuery!.ThenByDescending(t => EF.Property<object>(EF.Property<object>(t!, navigation1[0]), navigation1[1]) ?? EF.Property<object>(EF.Property<object>(t!, navigation2[0]), navigation2[1]));
                                else
                                    orderedQuery = orderedQuery!.ThenBy(t => EF.Property<object>(EF.Property<object>(t!, navigation1[0]), navigation1[1]) ?? EF.Property<object>(EF.Property<object>(t!, navigation2[0]), navigation2[1]));
                            }
                            else
                            {
                                if (column.Dir == "desc")
                                    orderedQuery = orderedQuery!.ThenByDescending(t => EF.Property<object>(t!, arr[0]) ?? EF.Property<object>(t!, arr[1]));
                                else
                                    orderedQuery = orderedQuery!.ThenBy(t => EF.Property<object>(t!, arr[0]) ?? EF.Property<object>(t!, arr[1]));
                            }
                        }
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        orderedQuery = query.NavigationOrderBy(column.Column, column.Dir!);                        
                    }
                    else
                    {
                        orderedQuery = orderedQuery!.NavigationOrderBy(column.Column, column.Dir!);
                    }
                }
            }
            return orderedQuery ?? query;
        }

        private static IOrderedQueryable<TModel> NavigationOrderBy<TModel>(this IOrderedQueryable<TModel> orderedQuery, string column, string direction)
        {
            var navigation = column.Split('.');
            if (navigation.Length == 2)
            {
                if (direction == "desc")
                    orderedQuery = orderedQuery!.ThenByDescending(t => EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]));
                else
                    orderedQuery = orderedQuery!.ThenBy(t => EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]));
            }
            else if (navigation.Length == 3)
            {
                if (direction == "desc")
                    orderedQuery = orderedQuery!.ThenByDescending(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]));
                else
                    orderedQuery = orderedQuery!.ThenBy(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]));
            }
            else if (navigation.Length == 4)
            {
                if (direction == "desc")
                    orderedQuery = orderedQuery!.ThenByDescending(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]), navigation[3]));
                else
                    orderedQuery = orderedQuery!.ThenBy(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]), navigation[3]));
            }
            else if (navigation.Length == 5)
            {
                if (direction == "desc")
                    orderedQuery = orderedQuery!.ThenByDescending(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]), navigation[3]), navigation[4]));
                else
                    orderedQuery = orderedQuery!.ThenBy(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]), navigation[3]), navigation[4]));
            }
            else
            {
                if (direction == "desc")
                    orderedQuery = orderedQuery!.ThenByDescending(t => EF.Property<object>(t!, navigation[0]));
                else
                    orderedQuery = orderedQuery!.ThenBy(t => EF.Property<object>(t!, navigation[0]));
            }
            return orderedQuery;
        }
        private static IOrderedQueryable<TModel> NavigationOrderBy<TModel>(this IQueryable<TModel> query, string column, string direction)
        {
            IOrderedQueryable<TModel> orderedQuery = query as IOrderedQueryable<TModel>;
            var navigation = column.Split('.');
            if (navigation.Length == 2)
            {
                if (direction == "desc")
                    orderedQuery = query!.OrderByDescending(t => EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]));
                else
                    orderedQuery = query!.OrderBy(t => EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]));
            }
            else if (navigation.Length == 3)
            {
                if (direction == "desc")
                    orderedQuery = query!.OrderByDescending(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]));
                else
                    orderedQuery = query!.OrderBy(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]));
            }
            else if (navigation.Length == 4)
            {
                if (direction == "desc")
                    orderedQuery = query!.OrderByDescending(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]), navigation[3]));
                else
                    orderedQuery = query!.OrderBy(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]), navigation[3]));
            }
            else if (navigation.Length == 5)
            {
                if (direction == "desc")
                    orderedQuery = query!.OrderByDescending(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]), navigation[3]), navigation[4]));
                else
                    orderedQuery = query!.OrderBy(t => EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(EF.Property<object>(t!, navigation[0]), navigation[1]), navigation[2]), navigation[3]), navigation[4]));
            }
            else
            {
                if (direction == "desc")
                    orderedQuery = query!.OrderByDescending(t => EF.Property<object>(t!, navigation[0]));
                else
                    orderedQuery = query!.OrderBy(t => EF.Property<object>(t!, navigation[0]));
            }
            return orderedQuery;
        }
    }
}
