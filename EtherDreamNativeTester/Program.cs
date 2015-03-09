/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 09.03.2015
 * Time: 11:40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using EtherDream;

namespace EtherDreamNativeTester
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello EtherDream");
            
            // TODO: Implement Functionality Here
            var devices = 0;//EtherDreamNative.GetCardNum();
            Console.WriteLine("Found " + devices + " devices");
            
            Console.WriteLine("Press any key to continue . . . ");
            Console.ReadKey(true);
        }
    }
}