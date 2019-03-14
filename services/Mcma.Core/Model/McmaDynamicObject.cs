using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using Mcma.Core.Logging;

namespace Mcma.Core
{
    public abstract class McmaDynamicObject : McmaExpandoObject, IMcmaObject
    {
        protected McmaDynamicObject()
        {
            Type = GetType().Name;
        }

        public string Type { get; set; }
    }
}