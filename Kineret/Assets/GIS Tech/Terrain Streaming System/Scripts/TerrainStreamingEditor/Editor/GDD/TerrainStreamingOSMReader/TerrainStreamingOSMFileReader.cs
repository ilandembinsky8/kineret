/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using UnityEngine;
using System.Collections.Concurrent;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingOSMFileReader  
    {
    }
    public class TerrainStreamingOSMData
    {
        [TerrainStreamingOSMProperty("version")]
        public string Version { get; set; }
        [TerrainStreamingOSMProperty("generator")]
        public string Generator { get; set; }
        [TerrainStreamingOSMProperty("copyright")]
        public string Copyright { get; set; }
        [TerrainStreamingOSMProperty("attribution")]
        public string Attribution { get; set; }
        [TerrainStreamingOSMProperty("license")]
        public string License { get; set; }

        public TerrainStreamingOSMBounds Bounds { get; set; }
        public ConcurrentDictionary<long, TerrainStreamingOSMNode> Nodes = new ConcurrentDictionary<long, TerrainStreamingOSMNode>();
        public ConcurrentDictionary<long, TerrainStreamingOSMWay> Ways { get; set; } = new ConcurrentDictionary<long, TerrainStreamingOSMWay>();
        public ConcurrentDictionary<long, TerrainStreamingOSMRelation> Relations = new ConcurrentDictionary<long, TerrainStreamingOSMRelation>();
 
        public void FillNodes(TerrainStreamingTileData container)
        {
            foreach (var wayDic in Ways)
            {
                foreach (var nref in wayDic.Value.Nds)
                {
                    var Id = long.Parse(nref.Ref.Trim());

                    TerrainStreamingOSMNode dicNode = new TerrainStreamingOSMNode();

                    if (Nodes.TryGetValue(Id, out dicNode))
                    {
                            wayDic.Value.Nodes.Add(dicNode);
                    }
                }
            }

            if(Relations.Count>0)
            {
                ConcurrentDictionary<long, TerrainStreamingOSMWay> Relations_Ways = new ConcurrentDictionary<long, TerrainStreamingOSMWay>();

                foreach (var MainRelation in Relations)
                {
                    foreach (var member in MainRelation.Value.members)
                    {

                        var member_Id = long.Parse(member.Value.reference);

                        TerrainStreamingOSMWay MemberWay = new TerrainStreamingOSMWay();

                        if (Ways.TryGetValue(member_Id, out MemberWay))
                        {
                            List<TerrainStreamingOSMNode> New_MemberWayNodes = new List<TerrainStreamingOSMNode>();

                            foreach (var subWay in MemberWay.Nds)
                            {

                                TerrainStreamingOSMNode subNode = new TerrainStreamingOSMNode();

                                if (Nodes.TryGetValue(long.Parse(subWay.Ref), out subNode))
                                {
                                    New_MemberWayNodes.Add(subNode);

                                }
                            }
                            MemberWay.Nodes.Clear();
                            MemberWay.Nodes.AddRange(New_MemberWayNodes);

                            MemberWay.Tags.Clear();
                            MemberWay.Tags.AddRange(MainRelation.Value.Tags);

                            Relations_Ways.TryAdd(member_Id, MemberWay);
                        }

                    }

                }
                Ways.Clear();
                Ways = Relations_Ways;
            }


        }

    }
    public class TerrainStreamingOSMParser
    {
        private Dictionary<Type, Dictionary<string, PropertyInfo>> _metaOsmElements;
        public TerrainStreamingOSMParser()
        {
            _metaOsmElements = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

            AddMetaOsmElement<TerrainStreamingOSMData>();
            AddMetaOsmElement<TerrainStreamingOSMBounds>();
            AddMetaOsmElement<TerrainStreamingNd>();
            AddMetaOsmElement<TerrainStreamingOSMNode>();
            AddMetaOsmElement<TerrainStreamingOSMTag>();
            AddMetaOsmElement<TerrainStreamingOSMWay>();
            AddMetaOsmElement<TerrainStreamingOSMRelation>();
            AddMetaOsmElement<TerrainStreamingOSMRelationMember>();
        }

        private void AddMetaOsmElement<T>()
        {
            var elementType = typeof(T);
            var properties = TerrainStreamingParserHelper.GetOsmProperties(elementType);
            _metaOsmElements.Add(elementType, properties);
        }

        private void ApplyAttributes(XmlAttributeCollection attributes, object obj)
        {
            var osmProperties = _metaOsmElements[obj.GetType()];

            foreach (XmlAttribute rootAttribute in attributes)
            {
                var attrName = rootAttribute.Name;
                var attrValue = rootAttribute.Value;

                if (osmProperties.ContainsKey(attrName))
                {
                    var property = osmProperties[attrName];
                    TerrainStreamingParserHelper.SetValue(obj, property, attrValue);
                }
            }
        }

        private object ParseChild(XmlElement xmlNode, TerrainStreamingOSMData osm)
        {
            var name = xmlNode.Name;
            var attrs = xmlNode.Attributes;
            var result = default(object);

            if (name == "bounds")
            {
                var bounds = new TerrainStreamingOSMBounds();

                ApplyAttributes(attrs, bounds);
                osm.Bounds = bounds;

                result = bounds;
            }
            else if (name == "node")
            {
                var node = new TerrainStreamingOSMNode();
                ApplyAttributes(attrs, node);
                if (!osm.Nodes.ContainsKey(long.Parse(node.Id)))
                    osm.Nodes.TryAdd(long.Parse(node.Id), node);

                foreach (XmlElement xmlNodeChildNode in xmlNode.ChildNodes)
                {
                    var childElement = ParseChild(xmlNodeChildNode, osm);

                    if (childElement.GetType() == typeof(TerrainStreamingOSMTag))
                    {
                        node.Tags.Add((TerrainStreamingOSMTag)childElement);
                    }
                }

                result = node;
            }
            else if (name == "tag")
            {
                var tag = new TerrainStreamingOSMTag();
                ApplyAttributes(attrs, tag);

                result = tag;
            }
            else if (name == "way")
            {
                var way = new TerrainStreamingOSMWay();
                ApplyAttributes(attrs, way);
                osm.Ways.TryAdd(long.Parse(way.Id),way);


                foreach (XmlElement xmlNodeChildNode in xmlNode.ChildNodes)
                {
                    var childElement = ParseChild(xmlNodeChildNode, osm);

                    if (childElement.GetType() == typeof(TerrainStreamingOSMTag))
                    {
                        way.Tags.Add((TerrainStreamingOSMTag)childElement);
                    }
                    else if (childElement.GetType() == typeof(TerrainStreamingNd))
                    {
                        way.Nds.Add((TerrainStreamingNd)childElement);
                    }
                }

                result = way;
            }
            else if (name == "nd")
            {
                var nd = new TerrainStreamingNd();
                ApplyAttributes(attrs, nd);

                result = nd;
            }
            else if (name == "relation")
            {
                var relation = new TerrainStreamingOSMRelation();
                ApplyAttributes(attrs, relation);

                if (!osm.Relations.ContainsKey(long.Parse(relation.Id)))
                    osm.Relations.TryAdd(long.Parse(relation.Id), relation);


                foreach (XmlElement xmlNodeChildNode in xmlNode.ChildNodes)
                {
                    var childElement = ParseChild(xmlNodeChildNode, osm);

                    if (childElement != null)
                    {
                        if (childElement.GetType() == typeof(TerrainStreamingOSMRelationMember))
                        {
                            var memeber = new TerrainStreamingOSMRelationMember();
                            memeber = (TerrainStreamingOSMRelationMember)childElement;
                            relation.members.TryAdd(long.Parse(memeber.reference), memeber);

                        }else
                        if (childElement.GetType() == typeof(TerrainStreamingOSMTag))
                        {
                            relation.Tags.Add((TerrainStreamingOSMTag)childElement);
                        }
                    }
                }
            }
            else if (name == "member")
            {
                var member = new TerrainStreamingOSMRelationMember();
                ApplyAttributes(attrs, member);

                result = member;
            }

            return result;
        }

        public TerrainStreamingOSMData ParseFromFile(string filePath)
        {
            var result = default(TerrainStreamingOSMData);

            using (var stream = new FileInfo(filePath).OpenRead())
            {
                result = ParseFromStream(stream);
            }

            return result;
        }

        public async Task<TerrainStreamingOSMData> ParseFromFileAsync(string filePath)
        {
            var result = default(TerrainStreamingOSMData);

            using (var stream = new FileInfo(filePath).OpenRead())
            {
                result = await ParseFromStreamAsync(stream);
            }

            return result;
        }

        public async Task<TerrainStreamingOSMData> ParseFromStreamAsync(Stream stream)
        {
            var fileSource = string.Empty;

            using (var reader = new StreamReader(stream))
            {
                fileSource = await reader.ReadToEndAsync();
            }

            return Parse(fileSource);
        }

        public TerrainStreamingOSMData ParseFromStream(Stream stream)
        {
            var fileSource = string.Empty;

            using (var reader = new StreamReader(stream))
            {
                fileSource = reader.ReadToEnd();
            }

            return Parse(fileSource);
        }

        public TerrainStreamingOSMData Parse(string xmlFileSource)
        {
            var osm = new TerrainStreamingOSMData
            {
                Nodes = new ConcurrentDictionary<long, TerrainStreamingOSMNode>(),
                Ways = new ConcurrentDictionary<long, TerrainStreamingOSMWay>(),
                Bounds = new TerrainStreamingOSMBounds(),
                Relations = new ConcurrentDictionary<long, TerrainStreamingOSMRelation>(),
            };
            var doc = new XmlDocument();
            doc.LoadXml(xmlFileSource);
            var root = doc.DocumentElement;

            ApplyAttributes(root.Attributes, osm);

            foreach (XmlElement xmlNode in root)
            {
                ParseChild(xmlNode, osm);
            }

            return osm;
        }
    }
    public class TerrainStreamingOSMRelation 
    {
        [TerrainStreamingOSMProperty("id")]
        public string Id { get; set; }
  
        public ConcurrentDictionary<long, TerrainStreamingOSMRelationMember> members;
        public List<TerrainStreamingOSMTag> Tags;

        public TerrainStreamingOSMRelation ()
        {
            members = new ConcurrentDictionary<long, TerrainStreamingOSMRelationMember>();
            Tags = new List<TerrainStreamingOSMTag>();
        }
    }
    public class TerrainStreamingOSMRelationMember
    {
        [TerrainStreamingOSMProperty("ref")]
        public string reference { get; set; }
        [TerrainStreamingOSMProperty("role")]
        public string role { get; set; }
        [TerrainStreamingOSMProperty("type")]
        public string type { get; set; }

        public List<TerrainStreamingNd> ways = new List<TerrainStreamingNd>();

    }
    public class TerrainStreamingOSMBounds
    {
        [TerrainStreamingOSMProperty("minlat")]
        public double MinLat { get; set; }
        [TerrainStreamingOSMProperty("minlon")]
        public double MinLon { get; set; }
        [TerrainStreamingOSMProperty("maxlat")]
        public double MaxLat { get; set; }
        [TerrainStreamingOSMProperty("maxlon")]
        public double MaxLon { get; set; }
    }
    public class TerrainStreamingNd
    {
        [TerrainStreamingOSMProperty("ref")]
        public string Ref { get; set; }
    }
    public class TerrainStreamingOSMNode
    {
        [TerrainStreamingOSMProperty("id")]
        public string Id { get; set; }
        [TerrainStreamingOSMProperty("visible")]
        public bool Visible { get; set; }
        [TerrainStreamingOSMProperty("version")]
        public int Version { get; set; }
        [TerrainStreamingOSMProperty("changeset")]
        public string ChangeSet { get; set; }
        [TerrainStreamingOSMProperty("timestamp")]
        public DateTime TimeStamp { get; set; }
        [TerrainStreamingOSMProperty("user")]
        public string User { get; set; }
        [TerrainStreamingOSMProperty("uid")]
        public string Uid { get; set; }
        [TerrainStreamingOSMProperty("lat")]
        public double Lat { get; set; }
        [TerrainStreamingOSMProperty("lon")]
        public double Lon { get; set; }
        public List<TerrainStreamingOSMTag> Tags { get; set; } = new List<TerrainStreamingOSMTag>();
        public TerrainStreamingOSMTag MainTag { get; set; } = new TerrainStreamingOSMTag();
    }
    public class TerrainStreamingOSMTag
    {
        [TerrainStreamingOSMProperty("k")]
        public string Attribute { get; set; }
        [TerrainStreamingOSMProperty("v")]
        public string Value { get; set; }
    }
    public class TerrainStreamingOSMWay
    {
        [TerrainStreamingOSMProperty("id")]
        public string Id { get; set; }
        [TerrainStreamingOSMProperty("visible")]
        public bool Visible { get; set; }
        [TerrainStreamingOSMProperty("version")]
        public int Version { get; set; }
        [TerrainStreamingOSMProperty("changeset")]
        public string ChangeSet { get; set; }
        [TerrainStreamingOSMProperty("timestamp")]
        public DateTime TimeStamp { get; set; }
        [TerrainStreamingOSMProperty("user")]
        public string User { get; set; }
        [TerrainStreamingOSMProperty("uid")]
        public string Uid { get; set; }
        public List<TerrainStreamingNd> Nds { get; set; } = new List<TerrainStreamingNd>();
        public List<TerrainStreamingOSMNode> Nodes { get; set; } = new List<TerrainStreamingOSMNode>();
        public List<TerrainStreamingOSMTag> Tags { get; set; } = new List<TerrainStreamingOSMTag>();
        public TerrainStreamingOSMTag MainTag { get; set; } = new TerrainStreamingOSMTag();
        public List<TerrainStreamingOSMRelation> Relations { get; set; } = new List<TerrainStreamingOSMRelation>();
    }
    internal class TerrainStreamingParserHelper
    {
        public static Dictionary<string, PropertyInfo> GetOsmProperties(Type type)
        {
            var fields = type.GetProperties()
                .Select(c => new
                {
                    Property = c,
                    Attribute = (c.GetCustomAttributes().FirstOrDefault() as TerrainStreamingOSMPropertyAttribute)?.Name
                })
                .Where(c => c.Attribute != null)
                .ToDictionary(c => c.Attribute, f => f.Property);
            return fields;
        }
        public static void SetValue(object instance, PropertyInfo property, string value)
        {
            if (property.PropertyType == typeof(double))
            {
                property.SetValue(instance, double.Parse(value.Replace(".", ",")));
            }
            else if (property.PropertyType == typeof(string))
            {
                property.SetValue(instance, value);
            }
            else if (property.PropertyType == typeof(int))
            {
                property.SetValue(instance, int.Parse(value));
            }
            else if (property.PropertyType == typeof(bool))
            {
                property.SetValue(instance, bool.Parse(value));
            }
            else if (property.PropertyType == typeof(DateTime))
            {
                var dt = DateTime.Parse(value);
                property.SetValue(instance, dt);
            }
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    internal class TerrainStreamingOSMPropertyAttribute : Attribute
    {
        public string Name { get; set; }

        public TerrainStreamingOSMPropertyAttribute(string name)
        {
            Name = name;
        }
    }
}