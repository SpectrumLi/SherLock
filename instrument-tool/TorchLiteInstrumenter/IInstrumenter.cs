using Mono.Cecil;
using System.Collections.Generic;

namespace TorchLiteInstrumenter
{
    public interface IInstrumenter
    {
        bool Instrument(IEnumerable<MethodDefinition> methods);
    }

}
