using System;

namespace NHibernate.Search.Engine
{
    public static class LoaderHelper
    {
        public static bool IsObjectNotFoundException(Exception e)
        {
            return e is ObjectNotFoundException;
        }
    }
}