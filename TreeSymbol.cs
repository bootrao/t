using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace TPY
{
    public static class TreeSymbol
    {
        private static double ioOffsetArr = 0;
        private static string typeNameTag = "";
        public static void BuildTreeView(string tpyFilePath, out List<TreeNode> nodes)
        {
            nodes = new List<TreeNode>();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(tpyFilePath);

            XmlNodeList symbolsList = xmlDoc.GetElementsByTagName("Symbol");

            foreach (XmlNode symbolNode in symbolsList)
            {
                // 获取变量名称
                XmlNode nameNode = symbolNode.SelectSingleNode("Name");
                String varName = nameNode.InnerText;

                // 判断是否为全局变量且存在IOffset节点
                bool varNameg = varName.Substring(0, 2) == ".g";
                bool hasIOffsetNode = symbolNode.SelectSingleNode("IOffset") != null;
                bool limtOffsetSize = false;
                //排除过大的偏移量
                if (hasIOffsetNode)
                {
                    XmlNode OffsetSize = symbolNode.SelectSingleNode("IOffset");
                    String varoffset = OffsetSize.InnerText;
                    limtOffsetSize = int.Parse(varoffset) < 65000;
                }

                if (hasIOffsetNode && varNameg && limtOffsetSize)
                {
                    // 解析IOffset节点并计算出偏移量
                    XmlNode offsSetNode = symbolNode.SelectSingleNode("IOffset");
                    String varoffset = offsSetNode.InnerText;
                    int ioOffset = int.Parse(varoffset);
                    double ioOffsetHalf = int.Parse(varoffset) / 2;

                    // 解析数组信息
                    List<int> arraySizes = new List<int>();
                    ParseArrayInfo(symbolNode.SelectSingleNode("Type").InnerText, ref arraySizes);
                    //变量大小
                    int varBitSize = int.Parse(symbolNode.SelectSingleNode("BitSize").InnerText);

                    int currentDimension = 0;
                    var currentIndexes = new int[arraySizes.Count];
                    // 判断变量类型及生成节点
                    TreeNode node;
                    if (arraySizes.Count > 0) // 数组类型
                    {
                        // 构建数组节点
                        int count = arraySizes[0];
                        node = new TreeNode(string.Format("{0}[{1}] ({2})", varName, string.Join(",", arraySizes), ioOffsetHalf.ToString()));

                        String varNameTag = symbolNode.SelectSingleNode("Type").InnerText;
                        int varTypeIndex = varNameTag.LastIndexOf("OF", varNameTag.Length - 1, varNameTag.Length);
                        String varTyprTag = varNameTag.Substring(varTypeIndex + 2 ,(varNameTag.Length - varTypeIndex - 2)).Trim();
                        typeNameTag = varTyprTag;
                        // 如果存在多个维度，递归构建子节点
                        if (arraySizes.Count > 1)
                        {
                            ioOffsetArr = ioOffsetHalf;
                            for (int i = 0; i < arraySizes[currentDimension]; i++)
                            {
                                var childNode = node.Nodes.Add(string.Format("[{0}]", i));
                                node.Tag = new MemoryAddress(ioOffset, varBitSize, varTyprTag, arraySizes[1]);
                                currentIndexes[currentDimension] = i;
                                BuildSubNodes(childNode, currentIndexes, arraySizes.ToList(), currentDimension + 1, varBitSize);
                            }
                        }
                        else
                        {
                            //仅存在一维，直接构建子节点
                            int singleBitSize = varBitSize / count;
                            for (int i = 0; i < count; i++)
                            {
                                TreeNode childNode = new TreeNode(string.Format("[{0}] ({1})", i, ioOffsetHalf.ToString()));
                                childNode.Tag = new MemoryAddress(ioOffset, varBitSize, varTyprTag, i);
                                node.Nodes.Add(childNode);                                

                                ioOffsetHalf = singleBitSize < 16 ? (ioOffsetHalf + 0.5) : 
                                    (singleBitSize ==16 ? (ioOffsetHalf + 1) : (ioOffsetHalf + 2));
                            }
                        }
                    }
                    else // 非数组类型
                    {
                        String varNameTag = symbolNode.SelectSingleNode("Type").InnerText;
                        int varTypeIndex = varNameTag.LastIndexOf("OF", varNameTag.Length - 1, varNameTag.Length);
                        String varTyprTag = varNameTag.Substring(varTypeIndex + 2, (varNameTag.Length - varTypeIndex - 2)).Trim();

                        varName = varName + " [" + symbolNode.SelectSingleNode("Type").InnerText + "]" + " (" + ioOffsetHalf.ToString() + ")";
                        node = new TreeNode(varName);                        
                        node.Tag = new MemoryAddress(ioOffset, varBitSize, varTyprTag, 0);                        
                    }
                    nodes.Add(node);
                }
            }
        }
        private static void BuildSubNodes(TreeNode parentNode, int[] currentIndexes, List<int> arraySizes,int currentDimension,int bitSize)
        {
            int count = arraySizes[currentDimension];

            for (int i = 0; i < count; i++)
            {
                // 构建当前维度的子节点
                var childNode = parentNode.Nodes.Add(string.Format("[{0}]", currentIndexes[currentDimension]));

                 if (currentDimension < arraySizes.Count - 1)
                {
                    // 设定下一层递归中的新下标并进入递归
                    currentIndexes[currentDimension + 1] = i;
                    BuildSubNodes(childNode, currentIndexes, arraySizes, currentDimension + 1, bitSize);
                }
                else
                
                {
                    // 最后一维则给节点设置相关标识
                    childNode.Text += $" ({ioOffsetArr})";
                    string tagName = parentNode.Tag != null ? $"{parentNode.Tag}" : "1";
                    childNode.Tag = new MemoryAddress(ioOffsetArr, bitSize, typeNameTag, 0);
                    ioOffsetArr++;
                }

                // 更新当前索引值
                currentIndexes[currentDimension]++;
            }
        }

        public static void ParseArrayInfo(string type, ref List<int> arraySizes)
        {
            int openBracketIndex = type.IndexOf('[');

            if (openBracketIndex != -1) // 判断是否是数组类型
            {
                string[] ranges =
                    Regex.Matches(type.Substring(openBracketIndex), @"\[([0-9]+)\.\.([0-9]+)\]")
                          .OfType<Match>()
                          .Select(m => m.Value)
                          .ToArray();

                foreach (string range in ranges)
                {
                    int rangeStart, rangeEnd;

                    Match match = Regex.Match(range, @"([0-9]+)");
                    rangeStart = int.Parse(match.Value);
                    match = match.NextMatch(); // 获取第二个匹配数字
                    rangeEnd = int.Parse(match.Value);

                    arraySizes.Add(rangeEnd - rangeStart + 1);
                }
            }
        }


    }
}
