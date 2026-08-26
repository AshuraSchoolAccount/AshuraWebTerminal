using System;

namespace Main 
{ 
    public class Program 
    { 
        public static string commands = "HELP, EXIT, "; 
        public static string fixedInput = ""; 
        
        static void LoadTerminal() 
        { 
            try 
            { 
                Console.Write("Enter command: ");
                string? userInput = Console.ReadLine(); 
                fixedInput = userInput != null ? userInput.ToUpper().Trim() : ""; 
            } 
            catch (Exception e) 
            { 
                Console.WriteLine("Code could not run due to: " + e.Message); 
            } 
        } 
        
        static void StartTerminal() 
        { 
            switch (fixedInput) 
            { 
                case "HELP": 
                    Console.WriteLine("Commands that you can run are: " + commands); 
                    break; 
                case "EXIT":
                    Console.WriteLine("Exiting terminal...");
                    Environment.Exit(0);
                    break;

                case "FETCH IP":
                    Console.WriteLine();
                    break;
                
                default: 
                    Console.WriteLine("Command is not a valid command. Enter the command help for a list of the current commands"); 
                    break; 
            } 
        } 
        
        public static void Main(string[] args) 
        { 
            Console.WriteLine("Terminal Loaded");
            
            while (true) 
            {
                LoadTerminal(); 
                StartTerminal();
                Console.WriteLine();
            } 
        } 
    } 
}
