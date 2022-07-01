using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using static System.Console;

namespace clicalc6
{
    class Program
    {
        static void Main( string[] arg )
        {
            WriteLine( "\nCLICALC V6 (now with more cylinders!)\n" );

            // Start with example if no input from cmd
            string example = "-86 ( 12^3 ( 0.5- -18,000 +9 ) / .2 ) ( 3 ( 1 - 1.5 * -3 ) 4.090 ) 7";
            WriteLine( $"EXAMPLE: \n{ example }" );
            Calculation introCalc = new Calculation( '$', example );
            if ( introCalc.outcome != null ) { introCalc.outcome.getOutput(); }

            while ( true )
            {
                WriteLine("\nNEW CALCULATION \nOperators: -+*/ \nParentheses: 3(2.412(.5*8) \nExponents: 2^5\nSpaces ignored");
                WriteLine("Exit with ':exit'\n");
                string? userInput = ReadLine();
                switch (userInput)
                {
                    case (null): continue;
                    case (":exit"): return;
                    default:
                        Calculation newCalc = new Calculation('$', userInput);
                        if (newCalc.outcome != null) { newCalc.outcome.getOutput(); }
                        continue;
                }
            }
        }
    }
    class Calculation
    {
        public Sequence? outcome { get; set; }
        internal int hiPorder { get; set; } = 0;
        private bool syntaxValid = true;
        // public Number? outcome;
        internal Calculation(char cha, string input)
        {
            // basicinput validation + cleaning. 
            string inputValidated = validateInput (input );
            Sequence inputSymbols = parseSymbols( inputValidated ); // Sort numbers & ops
            Sequence signsParsed = parseSigned( inputSymbols ); // Parse signed 
            Sequence statements = parseParentheses( signsParsed ); // Parse parentheses

            // Resolution loop ( resolooption )
            if (syntaxValid) { Sequence outcome = resolveOperations( statements ); }
        }
        private string validateInput( string input )
        {
            // prep output data
            string fixedInput = input;

            // collect target pattern groups
            List<List<string[]>> patternGroups = new List<List<string[]>> { 
                Rules.implicits, Rules.illegals };

            // run check-replace for pairs in group in groups
            string prevInput;
            int groupsPos = 0; int replacePos = 0;
            string[] replacePair; string match; string replace;

            foreach ( List<string[]> group in patternGroups )
            {
                while (replacePos < group.Count())
                {
                    prevInput = fixedInput;
                    replacePair = group[replacePos];
                    match = replacePair[0]; replace = replacePair[1];
                    fixedInput = Regex.Replace( fixedInput, match, replace );
                    replacePos++;

                    // stop immediately if illegal input
                    if ( groupsPos == 1 && fixedInput != prevInput )
                    {
                        WriteLine( "\n" + fixedInput );
                        syntaxValid = false; return fixedInput;
                    }
                }
                replacePos = 0; groupsPos++;
            }
            if ( fixedInput != input ) { WriteLine( $"\nIMPLICITS FIXED: \n{ fixedInput }\n" ); }
            return fixedInput;
        }
        private Sequence parseSymbols( string input )
        {
            Sequence outList = new Sequence(); // return group
            Sequence numList = new Sequence(); // number-segment group
            bool collectNum = false;
            int index = 1;

            foreach ( char c in input )
            {
                // enable collecting or disable and refresh collector
                if ( !collectNum && ( Char.IsDigit(c) == true || c == '.' )) { 
                    collectNum = true;
                }
                // if collecting and hit non-num, add new number & refresh collector
                else if ( collectNum && !( Char.IsDigit(c) == true || c == '.' ) )
                {
                    Number newNum = new Number();
                    newNum.parseNumber( numList );
                    outList.Add( newNum );  // this doesnt trigger if number unclosed at end input
                    collectNum = false; numList = new Sequence();
                }
                // if collecting number, add as number, else add symbol
                if ( collectNum )
                {
                    numList.Add( new Symbol(c) );
                    // if hit end range, close&add open number 
                    if (index == input.Count())
                    {
                        Number newNum = new Number();
                        newNum.parseNumber( numList );
                        outList.Add( newNum );
                    };
                }
                else { outList.Add( new Symbol(c) ); }
                index++;
            }
            return outList;
        }
        private Sequence parseSigned( Sequence symbolSeq )
        {
            Sequence outSequence = symbolSeq;
            int posit = 1; // start at first possible number-part

            while (posit < outSequence.Count() )
            {
                // get curr as number if type, prev symbol for sign
                Symbol sym = outSequence[posit];
                Number? num = sym as Number;
                Symbol sign = outSequence[posit - 1];

                if ( num != null )
                { // IF prev is sign
                    if ((sign.cha == '+' || sign.cha == '-') &&
                    // and prev starts sequence OR next-preceding is any operator
                    ((posit - 1 == 0) || (outSequence[posit - 2] is not Number)))
                    {
                        // replace old rsymbols with new of signed value
                        outSequence.Remove(sign); outSequence.Remove(num);
                        double val = (sign.cha == '-') ? 0 - num.value : num.value;
                        outSequence.Insert(posit - 1, new Number(val));
                        continue;
                    } // no incr. (two syms replaced by one)
                }
                posit++;
            }
            WriteLine($"OUSEQ: {outSequence.getOutput()}");
            return outSequence;
        }
        private Sequence parseParentheses(Sequence symbolSeq)
        {
            Sequence parenthSeq = new Sequence();

            int pLevel = 0;
            // get Symbol with pLevel (omit parenthesis itself)
            foreach (Symbol s in symbolSeq)
            {
                switch (s.cha)
                {
                    case ('('): pLevel++; continue;
                    case (')'): pLevel--; continue;
                    default: s.pLevel = pLevel; parenthSeq.Add(s); continue;
                }
            }
            bool collecting = false;
            int posit = 0; int assignOrd = 0;
            int targetPLev = parenthSeq.Max(x => x.pLevel);

            while (targetPLev > -1)
            {
                Symbol sym = parenthSeq[posit]; posit++;
                // if not collecting and hit target pLev, start collecting
                if (!collecting && (sym.pLevel == targetPLev)) { collecting = true; }
                // if collecting and lose target pLev, stop collecting
                if (collecting && (sym.pLevel != targetPLev)){
                    collecting = false;
                    // & increment order if break parenth.
                    if (sym.pLevel < targetPLev) { assignOrd++; hiPorder++; }}
                // assign if the weather is fine
                if (collecting && (sym.pLevel == targetPLev)) { sym.pOrder = assignOrd; }
                // sequence index exceeds endrange, stop collecting, decr. pTarget
                if (posit == parenthSeq.Count()) {
                    targetPLev--; posit = 0; 
                    collecting = false;}
            }
            return parenthSeq;
        }
        private Sequence resolveOperations(Sequence symbolSeq)
        {
            int pOrder = 0;
            Sequence outSequence = symbolSeq;
            // get first group at highest parenthesis-depth
            Sequence hiPSequence = new Sequence(outSequence.Where(sym => sym.pOrder == pOrder));

            
            // until last possible operation group (left, op, right) && statement group
            while ( hiPSequence.Count() >= 3 ) // last solveNum with have pOrder of hiPorder
            {
                WriteLine($"\nhiPSequence.getOutput {hiPSequence.getOutput()}");
                WriteLine( $"hiPSequence.Count { hiPSequence.Count() }, " +
                    $"pOrder({pOrder}):({hiPorder})hiPorder\n" );
                
                // seek PEMDAS-highest operator object in hiPstatement
                int posit = 0; int opGroupNum = 0;
                Symbol oprSymbol = hiPSequence[posit]; 
                while ( opGroupNum >=0 )
                { 
                    char[] pemGroup = Rules.pemdasGroups[opGroupNum];
                    oprSymbol = hiPSequence[posit]; posit++; WriteLine( $"oprSymbol.str: {oprSymbol.str}");
                    if ( pemGroup.Contains(oprSymbol.cha) ) { break; }
                    if ( posit !< hiPSequence.Count() ) { posit = 0; opGroupNum--; }
                }
                
                // replace successful operation group with result-valued number
                Sequence opGroup = new Sequence { hiPSequence[posit-1], oprSymbol, hiPSequence[posit+1] };
                double? resultNble = solve( opGroup );
                if ( resultNble.HasValue )
                {
                    WriteLine( $"resultNble.Value{resultNble.Value}" );
                    
                    foreach (Symbol sym in opGroup) { outSequence.Remove(sym); }
                    outSequence.Insert ( posit-1, new Number { value = resultNble.Value } );

                    // refresh parenthesis segment
                    hiPSequence = new Sequence(outSequence.Where(sym => sym.pOrder == pOrder));
                    continue;
                }
                else // if solve not successful: bad inputs, not bad developer
                { 
                    WriteLine( $"Invalid element in { hiPSequence.getOutput() }"); 
                    syntaxValid = false; return hiPSequence; 
                }

                // update Symbol sequence, run end-checks
                // if number = 1, place as number in highest-pOrder neighbour ( should be both? ), pOrder--, continue
                    // else probably input syntax issue; statements can only consist of odd numbers of elements (i think?)
                // if result, finish & output


            }
            return hiPSequence;
        }

        public double? solve( Sequence opGroup ) { //Symbol leftSym, Symbol oprtor, Symbol rightSym, 

            Sequence oprGroup = opGroup;

            Number solveNum = new Number ();
            Number? left = oprGroup[0] as Number; 
            Number? right = oprGroup[2] as Number;
            
            if ( left != null  && right != null ) {

                double x = left.value; 
                double y = right.value;
                return oprGroup[1].cha switch
                {
                    '^' => Math.Pow(x, y),
                    '*' => x*y,
                    '/' => ( y != 0 ) ? x/y : 0,
                    '+' => x+y,
                    '-' => x-y,
                    _ => null
                }; 
            }
            else {return null;}
        }
    }
    static class Rules
    {
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
            new string[] { @"(?<illeg>[^\s,0-9\^\/\*\+\-\(\)\.])",
                "...\nINVALID: >${illeg}< (illegal non-variable character)\n" },
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
        public static List<char[]> pemdasGroups = new List<char[]> {
            new char[] { '^' },
            new char[] { '*','/' },
            new char[] { '+', '-' } };
        };
        
    }
    class Sequence : Collection<Symbol>
    {
        public Sequence() { }
        public Sequence(IEnumerable<Symbol> iEnSymbols)
        {
            foreach (Symbol sym in iEnSymbols) { this.Add(sym); }
        }
        public string getOutput()
        {
            string output = "";
            foreach (Symbol sym in Items)
            {
                Number? symNum = sym as Number;
                output = (symNum != null) ? output + symNum.str : output + sym.str;
            }
            return output;
        }
    }
    class Number : Symbol
    {
        internal double value { get; set; }
        public new string? str { get; set; }
        public Number(char cha = '$') : base(cha) {}
        public Number(double val, char cha = '$') : base(cha)
        {
            value = val;
            str = value.ToString();
        }
        internal void parseNumber(Sequence numList)
        {
            // member chars to list, to string
            IEnumerable<char> numChars = numList.Select(x => x.cha).ToList();
            var numString = string.Join("", numChars);

            // parse double
            try { this.value = Convert.ToDouble(numString); this.str = value.ToString(); }
            catch (FormatException)
            {
                WriteLine($"Number parse failed: {numString} \n");
                syntaxValid = false;
            }
        }
    }
    class Symbol
    {
        internal bool syntaxValid = true;
        internal char cha { get; set; }
        public string str { get; set; }
        internal int pLevel { get; set; } = 0;
        internal int? pOrder { get; set; }
        public Symbol(char c)
        {
            cha = c;
            str = c.ToString();
        }
    }
