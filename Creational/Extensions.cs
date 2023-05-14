using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Creational;

public static class Extensions
{
    public static Step AsFailedStep(this Step step) => (Step)(-(Int32)step);
}
