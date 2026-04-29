using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Runners.Shared
{
    public static class InputNormalizer
    {
        public static List<JToken> NormalizeInputs(JToken? inputsToken, int paramCount, string testName)
        {
            if (paramCount == 0)
            {
                if (inputsToken is JArray arr && arr.Count > 0)
                    throw new InvalidOperationException(
                        $"Sample test '{testName}': entrypoint has no parameters, but inputs were provided.");

                return new List<JToken>();
            }

            if (inputsToken == null)
                throw new InvalidOperationException(
                    $"Sample test '{testName}': inputs are missing.");

            // SINGLE PARAM
            if (paramCount == 1)
            {
                if (inputsToken is JArray outer)
                {
                    if (outer.Count != 1)
                        throw new InvalidOperationException(
                            $"Sample test '{testName}': expected exactly 1 input argument, got {outer.Count}.");

                    return new List<JToken> { outer[0] };
                }

                // fallback — считаем, что это уже сам аргумент
                return new List<JToken> { inputsToken };
            }

            // MULTI PARAM
            if (inputsToken is not JArray arrMulti)
                throw new InvalidOperationException(
                    $"Sample test '{testName}': inputs must be an array of arguments.");

            if (arrMulti.Count != paramCount)
                throw new InvalidOperationException(
                    $"Sample test '{testName}': inputs count ({arrMulti.Count}) does not match parameter count ({paramCount}).");

            return arrMulti.ToList();
        }
    }
}
