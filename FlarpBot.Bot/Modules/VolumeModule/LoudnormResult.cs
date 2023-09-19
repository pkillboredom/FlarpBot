using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlarpBot.Bot.Modules.VolumeModule
{
    /**
     * From JSON:
     {
        "input_i" : "-18.80",
        "input_tp" : "2.99",
        "input_lra" : "3.80",
        "input_thresh" : "-29.11",
        "output_i" : "-23.16",
        "output_tp" : "-3.53",
        "output_lra" : "3.70",
        "output_thresh" : "-33.43",
        "normalization_type" : "dynamic",
        "target_offset" : "-0.84"
      }
    */
    public class LoudnormResult
    {
        public double input_i { get; set; }
        public double input_tp { get; set; }
        public double input_lra { get; set; }
        public double input_thresh { get; set; }
        public double output_i { get; set; }
        public double output_tp { get; set; }
        public double output_lra { get; set; }
        public double output_thresh { get; set; }
        public string normalization_type { get; set; }
        public double target_offset { get; set; }
    }
}
