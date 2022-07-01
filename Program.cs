using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using static System.Console;

namespace clicalc6 {
	
    class Program {
        static void Main(string[] args) {

            WriteLine( "CLICALC V6 (now with more cylinders!)" );

            // Start with example if no input from cmd
            string example = "-82 ( ( 0.5- -12,000 +2 ) / .2 ) ( 3 ( 1 - 1.5 * -2 ) 4.2 ) 7";
            WriteLine( $"EXAMPLE: \n{example}" );
            Calculation introCalc = new Calculation ( '$', example);
            // introCalc.showOutcome();

            while (true) {
                WriteLine("\nInput statements with operators: ,.()-+*/ (spaces ignored).");
                WriteLine("Exit with ':exit'");
                string? userInput = ReadLine();
                if ( userInput == null ) { userInput = "0"; }
                if ( userInput == ":exit" ) { break; }
                Calculation newCalc = new Calculation ( '$', userInput);
                // newCalc.showOutcome();
            }
        }
    }

    class Calculation {
        private bool syntaxValid = true;
        // public Number? outcome;
        internal Calculation ( char cha, string input ) {
            
            // basicinput validation + cleaning. 
            string inputValidated = validateInput( input );

            Sequence inputSymbols = parseSymbols( inputValidated ); // Sort numbers & ops
            Sequence signsParsed = parseSigned( inputSymbols ); // Parse signed 
            Sequence statements = parseParentheses( signsParsed ); // Parse parentheses

            // Resolution loop ( resolooption )
            if ( syntaxValid ) { Number outcome = resolveOperations( statements ); }
            
        }

        private string validateInput( string input ) {

            // prep output data
            string fixedInput = input;

            // collect target pattern groups
            List<List<string[]>> patternGroups = new List<List<string[]>> { 
                Rules.implicits, Rules.illegals };

            // run check-replace for pairs in group in groups
            string prevInput;
			int groupsPos = 0; int replacePos = 0;
            string[] replacePair; string match; string replace;

            foreach ( List<string[]> group in patternGroups ) {

                while ( replacePos < group.Count() ) {
                    prevInput = fixedInput;
                    replacePair = group[replacePos];
                    match = replacePair[0]; replace = replacePair[1];
                    fixedInput = Regex.Replace(fixedInput, match, replace);
                    replacePos++;

                    // stop immediately if illegal input
                    if ( groupsPos == 1 && fixedInput != prevInput ) { 
                        WriteLine( "\n" + fixedInput );
                        syntaxValid = false; return fixedInput; }
                }
                replacePos = 0; groupsPos++;
            }

            if ( fixedInput != input) { WriteLine( $"\nIMPLICITS FIXED: \n{fixedInput}" ); }
            return fixedInput;
        }
        
        private Sequence parseSymbols ( string input ) {
            
            Sequence outList = new Sequence(); // return group
            Sequence numList = new Sequence(); // number-segment group
            bool collectNum = false;
            int index = 1;

            foreach (char c in input) {

                // enable collecting or disable and refresh collector
                if ( !collectNum && ( Char.IsDigit(c) == true || c == '.' ) ) {
                    collectNum = true; }
                // if collecting and hit non-num, add new number & refresh collector
                else if ( collectNum &&  !(Char.IsDigit(c) == true || c == '.' ) ) {
                    Number newNum = new Number ( ); newNum.parseNumber( numList );

                    outList.Add( newNum );  // this doesnt trigger if number unclosed at end input

                    collectNum = false; numList = new Sequence(); }

                // if collecting number, add as number, else add symbol
                if ( collectNum ) { numList.Add( new Symbol ( c ));
                    // if hit end range, close&add open number 
                    if ( index == input.Count() ) { 
                        Number newNum = new Number(); newNum.parseNumber( numList );
                        outList.Add( newNum ); } }
                else { outList.Add( new Symbol( c ) ); }

                index++;

            }

            return outList;
        }

        private Sequence parseSigned( Sequence symbolSeq ) {

            Sequence outSequence = symbolSeq; // output sequence

            // string concatenation of symbol values    
            string getOutstring() { 
                var outStr = outSequence.Select( x => x.str ).ToString();
                return ( outStr != null ) ? outStr : "null"; }
            
            string seqString = getOutstring();
            string pattern; int pattPos = 0;

            while ( pattPos < Rules.signed.Count() ) {
                // get pattern and try match
                pattern = Rules.signed[pattPos];
                Match match = Regex.Match(seqString, pattern);

                // if match, get symbol from L+R, replace in outSequence
                if ( match.Success ) {
                    int index = match.Index==0 ? match.Index : match.Index+1;
                    Symbol sign = outSequence[index]; 
                    Number? number = outSequence[index+1] as Number;
                    if ( number !=null ) { Number signedNumber = new Number ( ) {
                        // signed value is 0-number if negative, else number
                        value = ( sign.cha == '+' ) ? number.value : 0-number.value }; }
                    else { WriteLine( "Index align issue at parseSigned" ); }
                }
                seqString = getOutstring(); pattPos++;
             }
            return outSequence;
        }
        
		private Sequence parseParentheses( Sequence symbolSeq ) {

            Sequence parenthSeq = new Sequence();

            int pLevel = 0;
            // get Symbol with pLevel (omit parenthesis itself)
            foreach ( Symbol s in symbolSeq ) { switch (s.cha) {
                    case ( '(' ): pLevel++; continue; 
                    case ( ')' ): pLevel--; continue;
                    default: s.pLevel = pLevel; parenthSeq.Add(s); continue; } }

            bool collecting = false;
            int posit = 0; int assignOrd = 0;
            int targetPLev = parenthSeq.Max( x => x.pLevel );

            while ( targetPLev > -1 ) { Symbol sym = parenthSeq[posit]; posit++;
                // if not collecting and hit target pLev, start collecting
                if ( !collecting && ( sym.pLevel == targetPLev ) ) { collecting = true; }
                // if collecting and lose target pLev, stop collecting
                if ( collecting && ( sym.pLevel != targetPLev ) ) { collecting = false; 
                    // & increment order if break parenth.
                    if ( sym.pLevel < targetPLev ) { assignOrd++; } }  
                // assign if the weather is fine
                if ( collecting && ( sym.pLevel == targetPLev ) ) { sym.pOrder = assignOrd; }
                // sequence index exceeds endrange, stop collecting, decr. pTarget
                if ( posit == parenthSeq.Count() ) {  
                    targetPLev--; posit = 0; collecting = false; }
            }
            return parenthSeq;
        }

        private Number resolveOperations( Sequence symbolSeq ) {

            Sequence outGroup = symbolSeq;
            List<Symbol> hiPGroup = new List<Symbol> ();
            Number outcome = new Number ('#') { };

            int pOrder = 0;
			
			while ( pOrder > -1 ) {
				
				//NOT RECOGNIZING SIGN PARSED FOR 12000, CHECK FOR 82.
				//NOT RECOGNIZING SIGN PARSED FOR 12000, CHECK FOR 82.
				//NOT RECOGNIZING SIGN PARSED FOR 12000, CHECK FOR 82.
				//NOT RECOGNIZING SIGN PARSED FOR 12000, CHECK FOR 82.

				// get first group at highest parenthesis-depth
                hiPGroup = outGroup.Where( sym => sym.pOrder == pOrder ).ToList();

                WriteLine( $"outGroup OUTPUT: {outGroup.getOutput()} (parenth is dropped)" ); 
                
                // string[] hiPStrs = hiPGroup.Select( x => x.str ).ToString();
                // if ( outStr != null ) { WriteLine ( $"hiPGroup: { outStr.ToString() }" ); } 
                // else { WriteLine( "hiPGroup NULL" ); }

                foreach (Symbol sym in hiPGroup ) { 
                    Number? num = sym as Number;  
                    if ( num != null ) {
                        WriteLine( $"Order: { num.pOrder} : {num.str} "); }
                    else {
                        WriteLine( $"Order: { sym.pOrder} : {sym.str} "); }
                }

				// get hi-PEMDAS operator position in hiPstatement
				//	// add exponents top of MDAS ops
				
				// get operands relating to operator
				
				// get result from operation
                // Number operationValue = new Number ( '$' ).parseSequence( hiPGroup) );
				
				// update Symbol sequence, run end-checks
				
				// if result, finish & output

                pOrder--;

            }

            return outcome;
        }

        public void showOutcome( ) { 
            WriteLine( "==================================================================" );
            // WriteLine( $"OUTCOME: {outcome.str}\n\n" ); 
        }

    }
    static class Rules {
        public static List<string[]> implicits = new List<string[]> {
            // IMPLICIT regex match-replace patterns: { @"(matches)", "$replaces" }
            //  replace whitespace and thousandcomma with empty (strip whitespace and comma)
            new string[] { @"[\s,]+" , "" },
            //  insert * after a close that precedes open-parenth (implicit multiplication)
            new string[] { @"(?<cls>[\)])(?<opn>[\(])", "${cls}"+"*"+"${opn}" },
            //  zero in an empty open-close pair
            new string[] { @"(?<opn>[\(])(?<cls>[\)])", "${opn}"+"0"+"${cls}" },
            //  insert * after a number that precedes open-parenth (implicit multiplication)
            new string[] { @"(?<num>[0-9])(?<opn>[\(])", "${num}"+"*"+"${opn}" },
            //  insert * before a number that follows close-parenth (implicit multiplication)
            new string[] { @"(?<cls>[\)])(?<num>[0-9])", "${cls}"+"*"+"${num}" },
            //  insert 0 before a decimal that is not preceeded by a number
            new string[] { @"(?<notnum>[^0-9])(?<dec>[\.])", "${notnum}"+"0"+"${dec}" }
        };
        public static List<string[]> illegals = new List<string[]> {
            // ILLEGAL patterns
            //  identify any not(legal character class)
            new string[] { @"(?<illeg>[^\s,0-9\/\*\+\-\(\)\.])", 
                "...\nINVALID: >${illeg}< (illegal character)\n" },
            //  more than two plus/minus (allows for operations on signed numbers)
            new string[] { @"(?<illeg>[\+\-][\+\-][\+\-])", 
	            "...\nINVALID: >${illeg}< (more than two consecutive '+' or '-')\n" },
            //  more than one plus/minus following mul/div operator (allows mul/div on signed)
            new string[] { @"(?<illeg>[\*/][\+\-][\+\-])", 
	            "...\nINVALID: >${illeg}< (more than one '+' or '-'"
                + " following a '*' or '/')\n" },
            //  A mul/div follows '+' or '-' (eg. 1+/2)
            new string[] { @"(?<illeg>[\+\-][\*/])", 
	            "...\nINVALID: >${illeg}< ('*' or '/' follows '-' or '+')\n" }
        };
        public static List<string> signed = new List<string> {
                @"[\)\(\+\-\*/][\+\-]", // operator/parenth, sign and number
                @"^([\+\-])" }; // sign and number at start of input
    }
    class Sequence : Collection<Symbol> {
        public string getOutput () {
            string output = "";
            foreach ( Symbol sym in Items ) {
                Number? symNum = sym as Number;
                output = ( symNum !=null ) ? output + symNum.str : output + sym.str;
            }
            return output;
        }
     } 
    class Number : Symbol {
        internal double value { get; set; }
        public new string? str { get; set; }

        public Number ( char cha='$' ) : base ( cha ) {
        }
        internal void parseNumber( Sequence numList ) {
            // member chars to list, to string
            IEnumerable<char> numChars = numList.Select( x => x.cha ).ToList();
            var numString = string.Join("", numChars );

            if ( numString != null ) WriteLine( numString ); 
            else { WriteLine( "NUMSTRING NULL ON NEWNUM PARSESEQ" ); } // syntaxInvalid?

            // parse double
            try { this.value = Convert.ToDouble(numString); this.str = value.ToString(); }
            catch ( Exception exc) { 
                WriteLine($"Number parse failed: {numString} \n{exc}"); 
                syntaxValid = false; 
                }
        }
    }
    class Symbol {
        internal bool syntaxValid = true;
        internal char cha { get; set; }
        public string str { get; set; }
        internal int pLevel { get; set; } = 0;
        internal int? pOrder { get; set; }

        public Symbol ( char c ) {
            cha = c;
            str = c.ToString();

        }
        
    }
}