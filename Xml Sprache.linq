<Query Kind="Program">
  <NuGetReference>Sprache</NuGetReference>
  <Namespace>Sprache</Namespace>
</Query>

void Main()
{
	var xml =
	@"<canada>
  <bank>
    <name>AMEX BANK OF CANADA</name>
    <id>303</id>
    <branches>
      <branch>
        <rte>030300012</rte>
        <rtp>00012-303</rtp>
        <branchName/>
        <address>101 McNabb Street Markham, ON L3R 4H8</address>
      </branch>
      <branch>
        <rte>030300022</rte>
        <rtp>00022-303</rtp>
        <branchName/>
        <address ann=""2"">101 McNabb Street Markham, ON L3R 4H8</address>
      </branch>
	  <branch></branch>
      <branch>
	  	<!--can i have comment?-->
        <rte>030300032<!--and here--></rte>
        <rtp><!--and here?-->00032-303</rtp>
        <branchName  att=""2"" />
        <address>101 McNabb Street 101 McNabb Street, Markham, ON L3R 4H8</address>
      </branch>
    </branches>
  </bank>
  </canada>";

	xmlParser.Content.Parse("").Dump("test1");
	xmlParser.Element.Parse(@"<branch>
        <rtp>00032-303</rtp>
        <branchName ann=""2""/>
        <address>101 McNabb Street 101 McNabb Street, Markham, ON L3R 4H8</address>
      </branch>").Dump("test2");

	xmlParser.Element.Parse(xml).Dump("Finale!");



	// only thing i can't parse correctly is xml namespaces yet.
	//xmlParser.Element.Parse(@"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""></xs:schema>");

	var a = XElement.Parse(@"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""></xs:schema>").Dump();

	// wow, this was a complex element...
	new XElement(
		XName.Get("schema", "http://www.w3.org/2001/XMLSchema"),
		new object[] {
			// interesting, xmlns is really an alias for a specific namespace... never thought about it.
			new XAttribute(XName.Get("xs", "http://www.w3.org/2000/xmlns/"), "http://www.w3.org/2001/XMLSchema"),
			"" // must have empty content to not be self-closing element.
			}
	).Dump();
}


class xmlParser
{
	public class OpenTagData
	{
		public string name { get; set; }
		public IEnumerable<XAttribute> attribs { get; set; }
	}

	/*
		Make Regular Language
		X == Xml
	 	X = <Ns:Tag (ns:prop)* ( /> 
							   | >( Content 
								  | (Comment|X)*
								  )
								 </Tag>
							   )
							   
		need ns, tag, prop, content, comment
		
		and the question is can i save/update a namespace dictionary in the middle of parsing?
		i think so, if i hack with "let".
		
		now i just need to code this :)
	*/

	public static Parser<string> AllLetters = Parse.Letter.Many().Text();

	public static Parser<XAttribute> Attrib =
		(from name in AllLetters
		 from equalAndLQuote in Parse.String("=\"")
		 from content in Parse.CharExcept('"').Many().Text()
		 from RQuote in Parse.Char('"')
		 select new XAttribute(name, content)
		).Token();

	public static Parser<XElement> SelfClosingElement =
		from opener in Parse.Char('<').Token()
		from tag in AllLetters
		from attribs in Attrib.Many()
		from closer in Parse.String("/>").Token()
		select new XElement(tag, attribs);

	public static Parser<OpenTagData> OpenTag =
		from opener in Parse.Char('<').Token()
		from name in AllLetters
		from attribs in Attrib.Many()
		from closer in Parse.Char('>').Token()
		select new OpenTagData { name = name, attribs = attribs };

	public static Parser<string> CloseTag(string name)
	{
		return
			from opener in Parse.String("</").Token()
			from tag in Parse.String(name).Text()
			from closer in Parse.Char('>')
			select "";
	}

	// content wont parse <![CDATA[]]>, get to that later.
	public static Parser<string> Content = Parse.CharExcept('<').Many().Text();



	public static Parser<XComment> Comment =
	from open in Parse.String("<!--")
	from text in Parse.AnyChar.Except(Parse.String("-->")).Many().Text()
	from close in Parse.String("-->")
	select new XComment(text);


	// main parser
	public static Parser<XElement> Element =
		from tag in OpenTag
			//from content in Content // not html worthy, but works for now.
		from children in Content
							.Or<object>(Element)
							.Or(SelfClosingElement)
							.Or(Comment)
							.Many()
		from close in CloseTag(tag.name)
		select new XElement(tag.name, new object[] { tag.attribs, children });




	// trying to add parsing for xml namespaces now.

	public class XmlnsInfo
	{
		public string Alias { get; set; }
		public string FullName { get; set; }
	}

	//
	public static Dictionary<string, string> XmlNameSpaces =
		new Dictionary<string, string>()
		{
			{"xmlns","http://www.w3.org/2000/xmlns/"} // the namespace of the xmlns alias.
		};


	// thoughts for parsing xml namespace.

	// leading namespace.
	// what should be returned from this?
	// i somehow need the full xmlns name.....
	// i guess, i'll keep a static dictionary, that i'll update on parsing an xmlnamespace...
	public static Parser<XmlnsInfo> Xmlns =
		from xmlns in AllLetters
		from colon in Parse.Char(':')
		select new XmlnsInfo { Alias = xmlns, FullName = XmlNameSpaces[xmlns] };


	// so, to parse sucessfully, i should do a 1st pass, just to collect namespaces,
	// then on second pass, i'll parse the xml.

	// we'll need to parse:
	// xmlns:SomeName="the crazy name space"
	// xmlns="the crazy name space"

}
