using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using static System.Console;

namespace clicalc
{
    class Program
    {
        static bool isFirstRun { get; set; } = true;
        static void Main(string[] args)
        {
            runIntro();
            while (true)
            {
                showSeparator(1, 1, 1);
                ParsedInput parsedInput = new ParsedInput(isFirstRun, args);
                if (!parsedInput.inputValid) { continue; }
                SolvedSequence solution = new SolvedSequence(parsedInput.parsed);
                solution.showOutcome();
                isFirstRun = false;
            }
        }
        static void runIntro()
        {
            showSeparator(1, 1, 0);
            WriteLine("\nCLICALC V6 (now with more cylinders!)");
            WriteLine(""
                + "\nOperators: -+*/"
                + "\nParentheses: 3(2.412(.5*8)"
                + "\nExponents: 2^5\nSpaces ignored"
                + "\nExit with ':exit'\n");
            showSeparator(0, 1, 1);
        }
        static void showSeparator(int aboveSp = 0, int lines = 1, int belowSp = 0)
        {
            for (int i = 0; i < aboveSp; i++) { WriteLine("\n"); }
            for (int i = 0; i < lines; i++) { WriteLine("===================================="); }
            for (int i = 0; i < belowSp; i++) { WriteLine("\n"); }
        }
    }
    class SolvedSequence : Sequence
    {
        internal Sequence? hiPSequence { get; set; } = null;
        public Sequence? solution { get; set; } = null;
        public SolvedSequence(Sequence mainSequence)
        {
            // Resolution loop ( resolooption )
            while (syntaxValid && solution == null)
            {
                (Sequence operatGroup, hiPSequence) = getOperation(mainSequence);
                Sequence hiPSequenceUpdated = solveOperation(operatGroup, hiPSequence);
                mainSequence = updateSequence(hiPSequenceUpdated, mainSequence);
            }
        }
        private (Sequence, Sequence) getOperation(Sequence mainSequence)
        {
            int? currPOrder = mainSequence.OrderBy(sym => sym.pOrder).First().pOrder;
            if (currPOrder != null) { currPOrder = currPOrder.Value; }

            Sequence hiPSequence = new Sequence(mainSequence.Where(sym => sym.pOrder == currPOrder));

            int pemGroupIDX = 0; int pemChaIDX = 0;
            Sequence foundOperators = new Sequence();
            while (pemGroupIDX < Rules.pemdasGroups.Count())
            {
                char pemCha = Rules.pemdasGroups[pemGroupIDX][pemChaIDX];
                foreach (Symbol sym in hiPSequence)
                {
                    if (sym.cha == pemCha) { foundOperators.Add(sym); }
                }
                pemChaIDX++;
                if (pemChaIDX == Rules.pemdasGroups[pemGroupIDX].Count())
                {
                    if (foundOperators.Count() != 0)    // return found operand-group
                    {
                        Symbol soonestPemHi = foundOperators.OrderBy(
                            s => hiPSequence.IndexOf(s)).First();
                        return (new Sequence
                        {
                            hiPSequence[hiPSequence.IndexOf(soonestPemHi)-1],
                            soonestPemHi,
                            hiPSequence[hiPSequence.IndexOf(soonestPemHi)+1]
                        }, hiPSequence);
                    }
                    else { pemGroupIDX++; pemChaIDX = 0; }
                }
            }
            return (foundOperators, hiPSequence);       // default return empty opseq
        }
        private Sequence solveOperation(Sequence opertnGroup, Sequence hiPSequence)
        {
            if (hiPSequence.Count() == 1) { return hiPSequence; }
            Sequence outSequence = new Sequence(hiPSequence);
            Number? left = opertnGroup[0] as Number;
            Symbol operatr = opertnGroup[1];
            Number? right = opertnGroup[2] as Number;
            if (left != null && right != null)
            {
                double x = left.value;
                double y = right.value;
                double? groupValue = operatr.cha switch
                {
                    '^' => Math.Pow(x, y),
                    '*' => x * y,
                    '/' => (y != 0) ? x / y : 0,
                    '+' => x + y,
                    '-' => x - y,
                    _ => null
                };
                if (groupValue != null)
                {
                    //insert new num with value in statement at opr. posit, remove opr.
                    int insertIdx = outSequence.IndexOf(left);
                    foreach (Symbol sym in opertnGroup) { outSequence.Remove(sym); }
                    outSequence.Insert(insertIdx,
                        new Number(groupValue.Value) { pOrder = operatr.pOrder });
                }
            }
            return outSequence;
        }
        private Sequence updateSequence(Sequence hiPSequenceUPD, Sequence inSequence)
        {
            Sequence outSequence = new Sequence(inSequence);
            // guard against null collectors
            if (hiPSequence == null || hiPSequenceUPD == null)
            {
                WriteLine("NULL BOTH PSEQ"); syntaxValid = false; return outSequence;
            }
            // ELSE substitute solve sequence into outsequence
            int insertIDX = outSequence.IndexOf(hiPSequence[0]);
            foreach (Symbol sym in hiPSequence) { outSequence.Remove(sym); }
            foreach (Symbol sym in hiPSequenceUPD)
            {
                outSequence.Insert(insertIDX, sym); insertIDX++;
            }
            // If final value; return as outcome
            if ((outSequence.Count() == 1) && (outSequence[0] is Number))
            {
                this.solution = outSequence; return outSequence;
            }
            // if hiPSeq is single value, takes pOrder of highest-pOrder neighbor
            if (hiPSequenceUPD.Count() == 1)
            {
                // if hip value starts or ends full sequence, get neighbour accordingly
                int hiPindex = outSequence.IndexOf(hiPSequenceUPD[0]);
                Symbol hiPNeighbour =
                    (hiPindex == 0) ? outSequence[hiPindex + 1] :
                    (hiPindex == outSequence.Count() - 1) ? outSequence[hiPindex - 1] :
                    // else favour highest-pOrder-neighbour
                    (outSequence[hiPindex - 1].pOrder > outSequence[hiPindex + 1].pOrder) ?
                        outSequence[hiPindex - 1] : outSequence[hiPindex + 1];

                outSequence[hiPindex].pOrder = hiPNeighbour.pOrder;
            }
            return outSequence;
        }
        public void showOutcome()
        {
            if (solution != null && syntaxValid == true)
            {
                // showSeparator(1, 1, 1);
                WriteLine($"CLICALC GOT: \n{solution.getOutput()}");
                // showSeparator(1, 2, 1);
            }
            else { WriteLine("Bad input. NAUGHTY input!"); } // showSeparator(1, 2, 1);
        }
    }
    class ParsedInput
    {
        internal int hiPorder { get; set; } = 0;
        internal bool inputValid { get; set; } = true;
        internal Sequence parsed { get; set; }
        internal Sequence? hiPSequence { get; set; }
        public ParsedInput(bool firstRun, string[] args)
        {
            // get and validate input, get sequence and parse sequence
            string input = getInput(firstRun, args);
            string inputValidated = validateInput(input);
            Sequence symbolsParsed = parseSymbols(inputValidated);
            Sequence signsParsed = parseSigned(symbolsParsed);
            parsed = parseParentheses(signsParsed);
        }
        private string getInput(bool firstRun, string[] args)
        {
            // if first run, use any args else example. Otherwise, get input.
            if (!firstRun)
            {
                while (true)
                {
                    WriteLine("NEW CALCULATION\n");
                    string? input = ReadLine();
                    if (input == null) { continue; }
                    else { return input; }
                }
            }
            if (args.Count() != 0)
            {
                string input = args[0].ToString();
                WriteLine($"FROM CMD: {input}");
                return input;
            }
            string example = "-86 (12^3( 0.5- -18,000+9 ) /.2 )(3 ( 1 -1.5 *-3)4.090 ) 7";
            WriteLine($"EXAMPLE: \n{example}\n");
            return example;

        }
        private string validateInput(string input)
        {
            string fixedInput = input;

            // run check-replace for pairs in group in groups
            List<List<string[]>> patternGroups = new List<List<string[]>> {
                Rules.implicits, Rules.illegals };
            int groupsPos = 0; int replacePos = 0;
            string[] replacePair; string match; string replace;
            foreach (List<string[]> group in patternGroups)
            {
                while (replacePos < group.Count())
                {
                    string prevInput = fixedInput;
                    replacePair = group[replacePos];
                    match = replacePair[0]; replace = replacePair[1];
                    fixedInput = Regex.Replace(fixedInput, match, replace);
                    replacePos++;

                    // stop immediately on illegal input
                    if (groupsPos == 1 && fixedInput != prevInput)
                    {
                        WriteLine("\n" + fixedInput);
                        inputValid = false; return fixedInput;
                    }
                }
                replacePos = 0; groupsPos++;
            }
            if (fixedInput != input) { WriteLine($"\nIMPLICITS FIXED: \n{fixedInput}"); }
            return fixedInput;
        }
        private Sequence parseSymbols(string input)
        {
            Sequence outList = new Sequence(); // return group
            Sequence numList = new Sequence(); // number-segment group
            bool collectNum = false;
            int index = 1;

            foreach (char c in input)
            {
                // enable collecting or disable and refresh collector
                if (!collectNum && (Char.IsDigit(c) == true || c == '.'))
                {
                    collectNum = true;
                }
                // if collecting and hit non-num, add new number & refresh collector
                else if (collectNum && !(Char.IsDigit(c) == true || c == '.'))
                {
                    Number newNum = new Number();
                    newNum.parseNumber(numList);
                    outList.Add(newNum);
                    collectNum = false; numList = new Sequence();
                }
                // if collecting number, add as number, 
                if (collectNum)
                {
                    numList.Add(new Symbol(c));
                    // if hit end range, close&add open number 
                    if (index == input.Count())
                    {
                        Number newNum = new Number();
                        newNum.parseNumber(numList);
                        outList.Add(newNum);
                    };
                }
                else { outList.Add(new Symbol(c)); } // else add as symbol
                index++;
            }
            return outList;
        }
        private Sequence parseSigned(Sequence inputSequence)
        {
            Sequence outSequence = inputSequence;
            int posit = 1; // start at first possible number-part

            while (posit < outSequence.Count())
            {
                // get curr as number if type, prev symbol for signF
                Symbol sym = outSequence[posit];
                Number? num = sym as Number;
                Symbol sign = outSequence[posit - 1];

                if (num != null)
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
            return outSequence;
        }
        private Sequence parseParentheses(Sequence parsedSigns)
        {
            Sequence parenthSeq = new Sequence();
            int pLevel = 0;
            foreach (Symbol s in parsedSigns)  // get p LEVEL
            {
                if (s.cha == '(') { pLevel++; continue; }
                else if (s.cha == ')') { pLevel--; continue; }
                else { s.pLevel = pLevel; parenthSeq.Add(s); continue; }
            }
            bool collecting = false;
            int posit = 0; int assignOrd = 0;
            int targetPLev = parenthSeq.Max(x => x.pLevel);
            while (targetPLev > -1)  // get group solve ORDER
            {
                Symbol sym = parenthSeq[posit]; posit++;
                // if not collecting and hit target pLev, start collecting
                if (!collecting && (sym.pLevel == targetPLev)) { collecting = true; }
                // if collecting and lose target pLev, stop collecting
                if (collecting && (sym.pLevel != targetPLev))
                {
                    collecting = false; // & increment order if break parenth.
                    if (sym.pLevel < targetPLev) { assignOrd++; hiPorder++; }
                }
                // assign if the weather is fine
                if (collecting && (sym.pLevel == targetPLev)) { sym.pOrder = assignOrd; }
                // sequence index exceeds endrange, stop collecting, decr. pTarget
                if (posit == parenthSeq.Count())
                {
                    targetPLev--; posit = 0; collecting = false;
                }
            }
            return parenthSeq;
        }
    }
    class Sequence : Collection<Symbol>
    {
        internal bool syntaxValid = true;
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
        public Number(char cha = '$') : base(cha) { }
        public Number(double val, char cha = '$') : base(cha)
        {
            value = val;
            str = value.ToString();
            base.str = str;
        }
        internal void parseNumber(Sequence numList)
        {
            string numString = numList.getOutput();

            try { this.value = Convert.ToDouble(numString); this.str = value.ToString(); }
            catch (FormatException)
            {
                syntaxValid = false;
            }
        }
        public void outputValue()
        {
            if (str == null) { str = value.ToString(); }
            WriteLine(str);
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
            new string[] { @"(?<notnum>[^0-9])(?<dec>[\.])", "${notnum}"+"0"+"${dec}" },
            //  insert 0 after a decimal that is not followed by a number
            new string[] { @"(?<dec>[\.])(?<notnum>[^0-9])", "${dec}"+"0"+"${notnum}" }
        };
        public static List<string[]> illegals = new List<string[]> {
            // ILLEGAL patterns
            //  identify any not(legal character class)
            new string[] { @"(?<illeg>[^\s,0-9\^\/\*\+\-\(\)\.])",
                "...\nINVALID: [ ${illeg} ] (illegal non-variable character)\n" },
            //  more than one consecutive mult/divide/expon/decimal
            new string[] { @"(?<illeg>[\.\^\*/]{2,})",
                " [...\nINVALID: >${illeg}< (more than one consecutive '*' '/' '^' or '.')\n...]" },
            //  more than two plus/minus (allows for operations on signed numbers)
            new string[] { @"(?<illeg>[\+\-][\+\-][\+\-])",
                " [...\nINVALID: >${illeg}< (more than two consecutive '+' or '-')\n...]" },
            //  more than one plus/minus following mul/div operator (allows mul/div on signed)
            new string[] { @"(?<illeg>[\*/][\+\-][\+\-])",
                " [...\nINVALID: >${illeg}< (more than one '+' or '-'"
                + " following a '*' or '/')\n...]" },
            //  A mul/div follows '+' or '-' (eg. 1+/2)
            new string[] { @"(?<illeg>[\+\-][\*/])",
                " [...\nINVALID: >${illeg}< ('*' or '/' follows '-' or '+')\n...]" },
            //  Any operator ends the sequence
            new string[] { @"(?<illeg>[\.\+\-\*/]\Z)",
                " [...\nINVALID: >${illeg}< (operator has no right-operand)\n...]" }
        };
        public static List<char[]> pemdasGroups = new List<char[]> {
            new char[] { '^' },
            new char[] { '*','/' },
            new char[] { '+', '-' }
        };
    }
}
