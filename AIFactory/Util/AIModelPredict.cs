using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.Util
{

    public class PythonCaller
    {
        public static string CallPythonLSTM(double[] input)
        {
            string inputArgs = string.Join(" ", input);
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"predict.py {inputArgs}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
        }
    }

}
