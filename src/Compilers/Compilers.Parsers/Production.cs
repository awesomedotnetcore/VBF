﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VBF.Compilers.Parsers
{
    public abstract class Production<T> : IProduction
    {
        public abstract void Accept(IProductionVisitor visitor);
    }
}
