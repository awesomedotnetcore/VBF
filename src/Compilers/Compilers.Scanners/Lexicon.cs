﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VBF.Compilers.Scanners.Generator;
using System.Collections.ObjectModel;

namespace VBF.Compilers.Scanners
{
    public class Lexicon
    {
        private List<Token> m_tokenList;
        private readonly LexerState m_defaultState;
        private List<LexerState> m_lexerStates;

        public Lexicon()
        {
            m_tokenList = new List<Token>();
            m_lexerStates = new List<LexerState>();
            m_defaultState = new LexerState(this, 0);

            m_lexerStates.Add(m_defaultState);
        }

        internal Token AddToken(RegularExpression definition, LexerState state, int indexInState)
        {
            int index = m_tokenList.Count;
            Token token = new Token(definition, this, index, state);
            m_tokenList.Add(token);

            return token;
        }

        public LexerState DefaultLexer
        {
            get
            {
                return m_defaultState;
            }
        }

        public ReadOnlyCollection<LexerState> GetLexerStates()
        {
            return m_lexerStates.AsReadOnly();
        }

        public ReadOnlyCollection<Token> GetTokens()
        {
            return m_tokenList.AsReadOnly();
        }

        public int LexerStateCount
        {
            get
            {
                return m_lexerStates.Count;
            }
        }

        public int TokenCount
        {
            get
            {
                return m_tokenList.Count;
            }
        }

        internal LexerState DefineLexerState(LexerState baseState)
        {
            int index = m_lexerStates.Count;
            LexerState newState = new LexerState(this, index, baseState);
            m_lexerStates.Add(newState);

            return newState;
        }

        public CompactCharSetManager CreateCompactCharSetManager()
        {
            var tokens = GetTokens();

            HashSet<char> compactableCharSet = new HashSet<char>();
            HashSet<char> uncompactableCharSet = new HashSet<char>();

            List<HashSet<char>> compactableCharSets = new List<HashSet<char>>();
            foreach (var token in tokens)
            {
                foreach (var getCharSetFunc in token.Definition.GetCompactableCharSets())
                {
                    compactableCharSets.Add(getCharSetFunc());
                }
                uncompactableCharSet.UnionWith(token.Definition.GetUncompactableCharSet());
            }

            foreach (var cset in compactableCharSets)
            {
                compactableCharSet.UnionWith(cset);
            }

            compactableCharSet.ExceptWith(uncompactableCharSet);

            Dictionary<HashSet<int>, ushort> compactClassDict = new Dictionary<HashSet<int>, ushort>(HashSet<int>.CreateSetComparer());
            ushort compactCharIndex = 1;
            ushort[] compactClassTable = new ushort[65536];

            foreach (var c in uncompactableCharSet)
            {
                ushort index = compactCharIndex++;
                compactClassTable[(int)c] = index;
            }

            foreach (var c in compactableCharSet)
            {
                HashSet<int> setofcharset = new HashSet<int>();
                for (int i = 0; i < compactableCharSets.Count; i++)
                {
                    var set = compactableCharSets[i];
                    if (set.Contains(c)) setofcharset.Add(i);
                }

                if (compactClassDict.ContainsKey(setofcharset))
                {
                    //already exist
                    ushort index = compactClassDict[setofcharset];

                    compactClassTable[(int)c] = index;
                }
                else
                {
                    ushort index = compactCharIndex++;
                    compactClassDict.Add(setofcharset, index);

                    compactClassTable[(int)c] = index;
                }
            }
            return new CompactCharSetManager(compactClassTable, compactCharIndex);
        }

        public ScannerInfo CreateScannerInfo()
        {
            DFAModel dfa = DFAModel.Create(this);
            CompressedTransitionTable ctt = CompressedTransitionTable.Compress(dfa);

            return new ScannerInfo(ctt.TransitionTable, ctt.CharClassTable, dfa.GetAcceptTables(), m_tokenList.Count);
        }
    }
}
