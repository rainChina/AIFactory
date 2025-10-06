using AIFactory.Model;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace AIFactory.Util
{
    public class SensorContext : DbContext
    {
        public DbSet<SensorData> SensorRecords { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=sensors.db");
    }

}
