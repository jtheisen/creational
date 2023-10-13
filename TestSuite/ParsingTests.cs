using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Creational;

[TestClass]
public class ParsingTests
{
    HeuristicTaxoboxParser parser = new HeuristicTaxoboxParser();

    [TestMethod]
    [DataRow("Peel & Stein, 2009", "[[John S. Peel|Peel]] & [[Martin Stein|Stein]], 2009")]
    [DataRow("Guillaumin", "[[André Guillaumin|Guillaumin]]")]
    [DataRow("Baz", "[[Foo|Bar|Baz]]")]
    [DataRow("ö", "[[ä|ü|ö]]")]
    [DataRow("foo", "[[foo]]")]
    [DataRow("a\nb", "[[a\nb]]")]
    public void TestDewikify(String expected, String original)
    {
        var actual = parser.Dewikify(original);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("foobar", "foo<!-- some xml comment -->bar")]
    [DataRow("foobar", "foo<!-- \nsome xml comment\n -->bar")]
    [DataRow("foobar -->", "foo<!-- some xml comment -->bar -->")]
    [DataRow("foo\nbar", "foo\n<!-- some xml comment -->bar")]
    public void TestXmlComments(String expected, String original)
    {
        var actual = parser.RemoveXmlComments(original);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    //[DataRow("{{Taxobox qwer}}", "dirt{{Taxobox qwer}}dirt\n\n}}")]
    //[DataRow("{{Taxobox foobar}}", "{{Taxobox foo<!-- \nsome xml comment\n -->bar}}")]
    [DataRow("""
        {{Taxobox
        | Taxon_Name       = Storchschnäbel
        | Taxon_WissName   = Geranium
        | Taxon_Rang       = Gattung
        | Taxon_Autor      = [[Carl von Linné|L.]]
        | Taxon2_Name      = Storchschnabelgewächse
        | Taxon2_WissName  = Geraniaceae
        | Taxon2_Rang      = Familie
        | Taxon3_Name      = Storchschnabelartige
        | Taxon3_WissName  = Geraniales
        | Taxon3_Rang      = Ordnung
        | Taxon4_Name      = Eurosiden II
        | Taxon4_Rang      = ohne
        | Taxon5_Name      = Rosiden
        | Taxon5_Rang      = ohne
        | Taxon6_Name      = Kerneudikotyledonen
        | Taxon6_Rang      = ohne
        | Bild             = Wiesenstorchschnabel.jpg
        | Bildbeschreibung = [[Wiesen-Storchschnabel]] (''{{lang|la|Geranium pratense}}'')
        }}
        """, "{{Taxobox\n| Taxon_Name       = Storchschnäbel\n| Taxon_WissName   = Geranium\n| Taxon_Rang       = Gattung\n| Taxon_Autor      = [[Carl von Linné|L.]]\n| Taxon2_Name      = Storchschnabelgewächse\n| Taxon2_WissName  = Geraniaceae\n| Taxon2_Rang      = Familie\n| Taxon3_Name      = Storchschnabelartige\n| Taxon3_WissName  = Geraniales\n| Taxon3_Rang      = Ordnung\n| Taxon4_Name      = Eurosiden II\n| Taxon4_Rang      = ohne\n| Taxon5_Name      = Rosiden\n| Taxon5_Rang      = ohne\n| Taxon6_Name      = Kerneudikotyledonen\n| Taxon6_Rang      = ohne\n| Bild             = Wiesenstorchschnabel.jpg\n| Bildbeschreibung = [[Wiesen-Storchschnabel]] (''{{lang|la|Geranium pratense}}'')\n}}\n\nDie '''Storchschnäbel''' oder '''Geranien''' (Einzahl ''Geranie''<ref>[https://www.duden.de/rechtschreibung/Geranium Geranium], [https://www.duden.de/rechtschreibung/Geranie Geranie] – ''[[Duden]].'' [[Bibliographisches Institut]], 2016.</ref> aus [[Nomenklatur (Biologie)|griechisch-lateinisch]] ''{{lang|la|Geranium}}'', dialektal auch ''Granium'') sind mit 380 bis 430 Arten die artenreichste [[Gattung (Biologie)|Gattung]] der [[Familie (Biologie)|Pflanzenfamilie]] der [[Storchschnabelgewächse]] (''{{lang|la|Geraniaceae}}''). Sie sind auf allen Kontinenten verbreitet.\n\nArten und Sorten der Gattung ''Geranium'' werden mindestens seit dem 16. Jahrhundert als [[Zierpflanze]]n kultiviert und Arten und vor allem [[Sorte (Pflanze)|Sorten]] sind in zahllosen Gärten und Parks anzutreffen.\n\n== Pelargonien und Geranien ==\n\nBis ins späte 18. Jahrhundert wurden auch die als Beet- und Balkonpflanzen beliebten [[Pelargonie]]n zur Gattung ''Geranium'' gezählt. Darauf weist der für diese Pflanzen noch heute in der Umgangssprache und im allgemeinen Handel gebräuchliche Begriff ''Geranien'' hin, der botanisch allerdings nicht korrekt ist. Denn Geranien (''Geranium'') und Pelargonien (''Pelargonium'') sind innerhalb der Storchschnabelgewächse zwei verschiedene Gattungen, die allerdings eng verwandt sind. So gibt es einige wenige Geranienarten, die sich wie Pelargonien durch weiche, filzige Stängel und große Rundblätter auszeichnen und damit den Arten dieser Gattung sehr ähnlich sehen. Einer der Unterschiede zwischen den beiden Gattungen ist: ''Geranium'' hat [[radiärsymmetrisch]]e [[Blüte]]n und ''Pelargonium'' hat [[zygomorph]]e Blüten.\n\n== Verbreitung ==\n\n=== Storchschnäbel – weltweit zuhause ===\n\n[[Datei:Geranium sessiliflorum0.jpg|mini|links|''[[Geranium sessiliflorum]]'' gehört zu den Storchschnabelarten, die auf [[Neuseeland]] und [[Tasmanien]] beheimatet sind]]\n\nStorchschnabelarten kommen auf allen Kontinenten und sogar in der [[Arktis]] und [[Antarktis]] vor. Sie sind außerdem in [[Südafrika]], [[Taiwan (Insel)|Taiwan]], [[Indonesien]], [[Neuguinea]], [[Australien (Kontinent)|Australien]], [[Tasmanien]], [[Neuseeland]], den Hawaii-Inseln, den [[Azoren]] und [[Madeira]] vertreten, wobei die eher kühleres Wetter bevorzugenden Geranien in diesen Regionen in der Regel in Gebirgsregionen wachsen.\n\nGeranium-Arten benötigen ein kühl-gemäßigtes Klima. Da in solchen Gebieten der Erde selten Trockenheit herrscht, sind viele der Storchschnabelarten gut auf feuchte Böden eingestellt. Aufgrund dieses Feuchtigkeitsbedürfnisses herrschen in den wärmeren Regionen ihres Verbreitungsgebietes einjährige Geranium-Arten vor, die ihre Wachstumszeit in der Regel im Winter haben und im Sommer als Samen ruhen.\n\n=== Standortanpassungen in Mitteleuropa heimischer Storchschnäbel ===\n\nDie meisten Storchschnabelarten bevorzugen basen- und stickstoffsalzreiche Lehmböden. Sie besiedeln häufig Ödlandflächen, Hackfruchtäcker, lückige Gebüsche und Rodungsflächen.\n\nInnerhalb dieses Standortspektrums zeigen die einheimischen Storchschnäbel artspezifische Anpassungen. Der [[Blutroter Storchschnabel|Blutrote Storchschnabel]] wächst in [[Europa]] bis nach Kleinasien in den sonnigen und lichten Waldrandbereichen und kommt dabei auch mit trockenen Böden zurecht. Der [[Wiesen-Storchschnabel]], dessen Verbreitungsgebiet von Europa bis nach Mittelasien und [[Sibirien]] reicht, ist dagegen eher an kühl-feuchten Standorten zu finden und wächst bevorzugt in den feuchten Senken von Wiesen und an Gräben. Der [[Wald-Storchschnabel]], der von Europa bis nach [[Westasien]] zu finden ist, wächst dort in bodenfeuchten Mischwäldern, auf frischen bis feuchten Bergwiesen und Hochstaudenfluren.\n\n=== Storchschnäbel als Neophyten, Archäophyten und Adventivpflanzen ===\n\nAufgrund ihrer Beliebtheit als Gartenpflanzen wurden Storchschnabelarten mittlerweile in viele Länder eingeführt, in denen sie ursprünglich nicht beheimatet waren. Der [[Rundblättriger Storchschnabel|Rundblättrige Storchschnabel]], den man in Mitteleuropa gelegentlich in [[Weinbau]]-Gebieten findet, ist vermutlich ursprünglich im Mittelmeerraum beheimatet gewesen. Heute ist er nahezu weltweit verbreitet.\n\nIn einigen Ländern haben die Storchschnabelarten so gute Ausgangsbedingungen gefunden, dass sie in sehr großem Maße verwildert sind und teilweise als Bioinvasoren angesehen werden. So wird das in Mitteleuropa beheimatete [[Ruprechtskraut]] an der Westküste der [[USA]] mittlerweile als unerwünschtes Unkraut eingeordnet. Auch der [[Pyrenäen-Storchschnabel]], den man in Mitteleuropa gelegentlich an Straßenrändern findet, ist als sogenannter [[Neophyt]] zu betrachten. Anders als das in den USA ungern gesehene Ruprechtskraut fristet er in Mitteleuropa eher ein Nischendasein.\n\nZu den mitteleuropäischen [[Archäophyt]]en gehört dagegen der [[Schlitzblättriger Storchschnabel|Schlitzblättrige Storchschnabel]]. Diese Storchschnabelart, die auf basen- und stickstoffsalzhaltigen Lehmboden wächst, ist ursprünglich im Mittelmeerraum beheimatet gewesen und zählt zu den [[Hemerochorie|hemerochoren]] Pflanzen, die mit den ersten Ackerbauern vermutlich über Saatgutverunreinigungen nach Mitteleuropa verschleppt wurden (sogenannte [[Speirochorie]]).\n\nDer [[Spreizender Storchschnabel|Spreizende Storchschnabel]] wird nur gelegentlich aus seinem Ursprungsgebiet, den warmen Tälern der West- und Südalpen wie dem [[Kanton Wallis|Wallis]] und dem [[Veltlin]] nach Mitteleuropa in Form von Samen verschleppt (sogenannte [[Agochorie]]). Er ist dann in der Lage, sich vorübergehend an dem neuen Standort zu etablieren. Er zählt daher zu den sogenannten [[Adventivpflanzen]].\n\n[[Datei:Illustration Geranium phaeum0.jpg|mini|Illustration: [[Brauner Storchschnabel]] (''{{lang|la|Geranium phaeum}}'')]]\n\n== Namensgebung, Beschreibung und Ökologie ==\n\nDie deutsche Bezeichnung „Storchschnabel“ erscheint beim ersten Blick auf die blühende Pflanze unverständlich. Der Fruchtstand erklärt jedoch den Namen: Die länglichen, eigentümlich gestalteten Fruchtstände erinnern an den [[Schnabel]] des Storches. Die botanische Bezeichnung ''Geranium'' basiert ebenfalls auf der Form der Fruchtstände; sie lässt sich auf das griechische Wort \"géranos\" (Kranich) zurückführen. Im Deutschen wurde die Pflanze Storchschnabel früher<ref>Petrus Uffenbach (Hrsg.): ''Pedacii Dioscoridis Anazarbaei Kraeuterbuch ...'' (ins Deutsche übersetzt von Johannes Danzius), Frankfurt am Main (bei Johann Bringern) 1610, S. 223.</ref> auch ''Kranichschnabel'' genannt.\n\n=== Die Pflanze ===\n\nStorchschnabel-Arten sind überwiegend [[Ausdauernde Pflanze|ausdauernde]], seltener [[Einjährige Pflanze|ein-]] oder [[Zweijährige Pflanze|zweijährige]] [[krautige Pflanze]]n, wenige Arten sind [[Halbstrauch|Halbsträucher]] oder [[Strauch|Sträucher]]. Sie enthalten [[ätherische Öle]].\nStorchschnabel-Arten wachsen buschig oder horstartig. In freier Natur sorgen die großen Blätter der Geranien und ihre häufig starke Breitenausdehnung dafür, dass sie im Vergleich zu konkurrierenden Pflanzenarten an ihrem Standort verhältnismäßig viel Nährstoffe und Wasser erhalten. Wie alle Familienmitglieder der Storchschnabelgewächse haben Storchschnabel-Arten gelenkartig verbundene [[Stängel]], die häufig [[Drüsenhaare]] haben. Einige Arten wie beispielsweise der [[Balkan-Storchschnabel]] sind nahezu immergrün, andere wie der [[Basken-Storchschnabel]] bilden während ihrer Blütezeit große, rundliche „Laubhügel“ aus, die während des Winterhalbjahrs verrotten.\n\n=== Die Blätter ===\n\nDie wechsel- oder gegenständigen, gestielten [[Blatt (Pflanze)|Laubblätter]] sind je nach Art unterschiedlich gestaltet. Bei einigen Arten gleicht das Blatt der bei den Pelargonien-Arten vorkommenden runden Form, bei den meisten Arten ist es jedoch fünfteilig und jeder Blattlappen stark eingekerbt. Stark geteilte Laubblätter hat beispielsweise ''Geranium purpureum''; bei dieser Art ist jedes Blatt in fünf Lappen unterteilt, die Teilung reicht dabei bis zur Blattachse. Zusätzlich ist jedes Blatt an der Spitze gelappt. Diese Blattform, die für viele der ''Geranium''-Arten typisch ist, bezeichnet man botanisch als tief fiederspaltig.\n\nBei den meisten Arten sind die Laubblätter einfarbig dunkelgrün, bei nur wenigen Arten treten unterschiedliche Grüntöne in der Blattfarbe auf. Die dunkelsten Laubblätter hat die auf [[Neuseeland]] und [[Tasmanien]] beheimatete Art ''Geranium sessiliflorum''. Bei einigen Sorten dieser Art wurde die ungewöhnliche Blattfärbung noch vertieft, sie ist fast dunkelviolett.\n\n[[Nebenblatt|Nebenblätter]] sind vorhanden.\n\n[[Datei:Geranium pratense (Meadow Cranesbill).jpg|mini|Zuchtsorte des [[Wiesen-Storchschnabel]]s mit blauen Blütenkronblättern]]\n\n=== Die Blüten ===\n\nDie Blüten stehen selten einzeln, meist zu zweit. Es ist in der Regel ein langer Blütenstiel vorhanden. Dies ermöglicht den Geranien an ihren natürlichen Standorten eine Konkurrenz zu den meist anderen, gleich hoch wachsenden Pflanzenarten von denen sie umgeben sind und auf diese Weise ihre [[Bestäubung]] sicherstellen.\n\nDie zwittrigen [[Blüte]]n sind [[radiärsymmetrisch]] und fünfzählig mit doppelter [[Blütenhülle]]. Die fünf grünen, freien und häufig behaarten [[Kelchblatt|Kelchblätter]] weisen stets eine vorspringende Spitze auf. Die Kelchblätter schließen zuerst die Blütenknospe ein. Wenn sich nach der Bestäubung aus der Blüte die Frucht entwickelt, vergrößern sich die Kelchblätter und schützen den Ansatz der entstehenden großen Frucht. Die fünf freien [[Kronblatt|Kronblätter]] sind bei manchen Arten genagelt. Die Farbe der Blütenkronblätter der Storchschnabelarten reicht von Weiß über Rosa und Purpurrot bis zu einem leuchtenden Blau. Bei vielen Arten und Sorten ist eine deutliche Maserung der Kronblätter erkennbar.\nEs sind zwei Kreise mit je fünf [[Staubblatt|Staubblättern]] vorhanden, sie sind alle [[fertil]]; bei den anderen Gattungen der Familie ist ein Teil der Staubblätter zu [[Staminodien]] reduziert. Die Ränder der [[Staubfäden]] sind behaart. Die meist fünf [[Nektarien]] des [[Diskus]] alternieren mit den Kronblättern, selten sind sie zu einem Ring vereinigt. Fünf [[Fruchtblatt|Fruchtblätter]] sind zu einem oberständigen [[Fruchtknoten]] verwachsen. Der Griffel endet in fünf Narben.\n\n[[Datei:Geranium pratense flowerdiagram.png|mini|links|160px|[[Blütendiagramm]] von ''[[Geranium pratense]]'']]\nDie Blütenformel lautet: <math>\\star K_{5} \\; C_5 \\; A_{5+5} \\; G_{\\underline{(5)}}</math>\n\nJedes einzelne Blütenkronblatt ist im Gegensatz zum Kelchblatt bei der überwiegenden Zahl der Arten am Ende abgerundet. Die Blütenform dagegen kann je nach Art")]
    [DataRow("""
        {{Taxobox
        | x = Anguilla-anguilla 1.jpg
        }}
        """, """
        dirt
                {{Taxobox
        | x = Anguilla-anguilla 1.jpg
        }}
        dirt        
        """)]
    public void TestTaxoboxRecognition(String expected, String original)
    {
        var actual = parser.GetTaxoboxWithHeuristicParsing(original);

        Assert.IsNotNull(actual, "No taxobox recognized");

        Assert.AreEqual(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
    }

    [TestMethod]
    [DataRow("""
        x = y
        """, """
        {{Taxobox
        | x = y
        }}
        """)]
    [DataRow("""
        x = y
        a = b
        """, """
        {{Taxobox
        | x = y
        noise
        just = noise
        | a = b
        }}
        """)]
    [DataRow("""
        foo = bar
        """, """
        {{Taxobox noise
        | foo = bar
        noise
        }}
        """)]
    [DataRow("""
        x = Anguilla-anguilla 1.jpg
        """, """
        {{Taxobox
        | x = Anguilla-anguilla 1.jpg
        }}
        """)]
    public void TestTaxoboxParsing(String expected, String original)
    {
        var actual = parser.ParseEntriesForTesting(original);

        Assert.AreEqual(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
    }

    [TestMethod]
    [DataRow("""
        no box
        """)]
    [DataRow("""
        {{Taxobox
        | novalue
        }}
        """)]
    [DataRow("""
        {{Taxobox | foo = bar
        }}
        """)]
    public void TestBrokenTaxoboxParsing(String broken)
    {
        Assert.ThrowsException<Exception>(() => parser.ParseEntriesForTesting(broken));
    }

    public static IEnumerable<Object[]> TestTaxonomyParsingData
    {
        get
        {
            yield return new Object[]
            {
                """
                Eukaryota,Eukaryoten,Domäne
                ,Lebewesen,Klassifikation
                """,
                """
                {{Taxobox
                | Taxon_Name       = Eukaryoten
                | Taxon_WissName   = Eukaryota
                | Taxon_Rang       = Domäne
                | Taxon_Autor      = [[Edouard Chatton|Chatton]], 1925
                | Taxon2_Name      = Lebewesen
                | Taxon2_Rang      = Klassifikation
                | Bild             = Eukaryota diversity 2.jpg
                | Bildbeschreibung = Verschiedene Eukaryoten
                }}
                """
            };
        }
    }

    [TestMethod]
    [DynamicData(nameof(TestTaxonomyParsingData))]
    public void TestTaxonomyParsing(String expected, String original)
    {
        var result = new ParsingResult();

        parser.ParseIntoParsingResult(result, original);

        var actual = String.Join("\n", result.TaxonomyEntries.Select(e => $"{e.Name},{e.NameLocal},{e.Rank}"));

        Assert.AreEqual(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
    }


    public static IEnumerable<Object[]> TestImageLinkParsingData
    {
        get
        {
            yield return new Object[]
            {
                """
                Binturong in Overloon.jpg
                Binturong2.jpg
                """,
                """
                [[Datei:Binturong in Overloon.jpg|mini|Der [[Binturong]] zählt zu den Schleichkatzen]]

                dirt [[Datei:Binturong2.jpg|mini|Der [[Binturong]]
                zählt immer noch zu den Schleichkatzen]]
                """
            };
        }
    }

    [TestMethod]
    [DynamicData(nameof(TestImageLinkParsingData))]
    public void TestImageLinkParsing(String expected, String original)
    {
        var actual = parser.FindImageLinksForTesting(original);

        Assert.AreEqual(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
    }

}
