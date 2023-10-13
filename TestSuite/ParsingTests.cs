using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Creational;

[TestClass]
public class ParsingTests
{
    HeuristicTaxoboxParser parser = new HeuristicTaxoboxParser();

    [TestMethod]
    [DataRow("Peel & Stein, 2009", "[[John S. Peel|Peel]] & [[Martin Stein|Stein]], 2009")]
    [DataRow("Guillaumin", "[[Andr� Guillaumin|Guillaumin]]")]
    [DataRow("Baz", "[[Foo|Bar|Baz]]")]
    [DataRow("�", "[[�|�|�]]")]
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
        | Taxon_Name       = Storchschn�bel
        | Taxon_WissName   = Geranium
        | Taxon_Rang       = Gattung
        | Taxon_Autor      = [[Carl von Linn�|L.]]
        | Taxon2_Name      = Storchschnabelgew�chse
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
        """, "{{Taxobox\n| Taxon_Name       = Storchschn�bel\n| Taxon_WissName   = Geranium\n| Taxon_Rang       = Gattung\n| Taxon_Autor      = [[Carl von Linn�|L.]]\n| Taxon2_Name      = Storchschnabelgew�chse\n| Taxon2_WissName  = Geraniaceae\n| Taxon2_Rang      = Familie\n| Taxon3_Name      = Storchschnabelartige\n| Taxon3_WissName  = Geraniales\n| Taxon3_Rang      = Ordnung\n| Taxon4_Name      = Eurosiden II\n| Taxon4_Rang      = ohne\n| Taxon5_Name      = Rosiden\n| Taxon5_Rang      = ohne\n| Taxon6_Name      = Kerneudikotyledonen\n| Taxon6_Rang      = ohne\n| Bild             = Wiesenstorchschnabel.jpg\n| Bildbeschreibung = [[Wiesen-Storchschnabel]] (''{{lang|la|Geranium pratense}}'')\n}}\n\nDie '''Storchschn�bel''' oder '''Geranien''' (Einzahl ''Geranie''<ref>[https://www.duden.de/rechtschreibung/Geranium Geranium], [https://www.duden.de/rechtschreibung/Geranie Geranie] � ''[[Duden]].'' [[Bibliographisches Institut]], 2016.</ref> aus [[Nomenklatur (Biologie)|griechisch-lateinisch]] ''{{lang|la|Geranium}}'', dialektal auch ''Granium'') sind mit 380 bis 430 Arten die artenreichste [[Gattung (Biologie)|Gattung]] der [[Familie (Biologie)|Pflanzenfamilie]] der [[Storchschnabelgew�chse]] (''{{lang|la|Geraniaceae}}''). Sie sind auf allen Kontinenten verbreitet.\n\nArten und Sorten der Gattung ''Geranium'' werden mindestens seit dem 16. Jahrhundert als [[Zierpflanze]]n kultiviert und Arten und vor allem [[Sorte (Pflanze)|Sorten]] sind in zahllosen G�rten und Parks anzutreffen.\n\n== Pelargonien und Geranien ==\n\nBis ins sp�te 18. Jahrhundert wurden auch die als Beet- und Balkonpflanzen beliebten [[Pelargonie]]n zur Gattung ''Geranium'' gez�hlt. Darauf weist der f�r diese Pflanzen noch heute in der Umgangssprache und im allgemeinen Handel gebr�uchliche Begriff ''Geranien'' hin, der botanisch allerdings nicht korrekt ist. Denn Geranien (''Geranium'') und Pelargonien (''Pelargonium'') sind innerhalb der Storchschnabelgew�chse zwei verschiedene Gattungen, die allerdings eng verwandt sind. So gibt es einige wenige Geranienarten, die sich wie Pelargonien durch weiche, filzige St�ngel und gro�e Rundbl�tter auszeichnen und damit den Arten dieser Gattung sehr �hnlich sehen. Einer der Unterschiede zwischen den beiden Gattungen ist: ''Geranium'' hat [[radi�rsymmetrisch]]e [[Bl�te]]n und ''Pelargonium'' hat [[zygomorph]]e Bl�ten.\n\n== Verbreitung ==\n\n=== Storchschn�bel � weltweit zuhause ===\n\n[[Datei:Geranium sessiliflorum0.jpg|mini|links|''[[Geranium sessiliflorum]]'' geh�rt zu den Storchschnabelarten, die auf [[Neuseeland]] und [[Tasmanien]] beheimatet sind]]\n\nStorchschnabelarten kommen auf allen Kontinenten und sogar in der [[Arktis]] und [[Antarktis]] vor. Sie sind au�erdem in [[S�dafrika]], [[Taiwan (Insel)|Taiwan]], [[Indonesien]], [[Neuguinea]], [[Australien (Kontinent)|Australien]], [[Tasmanien]], [[Neuseeland]], den Hawaii-Inseln, den [[Azoren]] und [[Madeira]] vertreten, wobei die eher k�hleres Wetter bevorzugenden Geranien in diesen Regionen in der Regel in Gebirgsregionen wachsen.\n\nGeranium-Arten ben�tigen ein k�hl-gem��igtes Klima. Da in solchen Gebieten der Erde selten Trockenheit herrscht, sind viele der Storchschnabelarten gut auf feuchte B�den eingestellt. Aufgrund dieses Feuchtigkeitsbed�rfnisses herrschen in den w�rmeren Regionen ihres Verbreitungsgebietes einj�hrige Geranium-Arten vor, die ihre Wachstumszeit in der Regel im Winter haben und im Sommer als Samen ruhen.\n\n=== Standortanpassungen in Mitteleuropa heimischer Storchschn�bel ===\n\nDie meisten Storchschnabelarten bevorzugen basen- und stickstoffsalzreiche Lehmb�den. Sie besiedeln h�ufig �dlandfl�chen, Hackfrucht�cker, l�ckige Geb�sche und Rodungsfl�chen.\n\nInnerhalb dieses Standortspektrums zeigen die einheimischen Storchschn�bel artspezifische Anpassungen. Der [[Blutroter Storchschnabel|Blutrote Storchschnabel]] w�chst in [[Europa]] bis nach Kleinasien in den sonnigen und lichten Waldrandbereichen und kommt dabei auch mit trockenen B�den zurecht. Der [[Wiesen-Storchschnabel]], dessen Verbreitungsgebiet von Europa bis nach Mittelasien und [[Sibirien]] reicht, ist dagegen eher an k�hl-feuchten Standorten zu finden und w�chst bevorzugt in den feuchten Senken von Wiesen und an Gr�ben. Der [[Wald-Storchschnabel]], der von Europa bis nach [[Westasien]] zu finden ist, w�chst dort in bodenfeuchten Mischw�ldern, auf frischen bis feuchten Bergwiesen und Hochstaudenfluren.\n\n=== Storchschn�bel als Neophyten, Arch�ophyten und Adventivpflanzen ===\n\nAufgrund ihrer Beliebtheit als Gartenpflanzen wurden Storchschnabelarten mittlerweile in viele L�nder eingef�hrt, in denen sie urspr�nglich nicht beheimatet waren. Der [[Rundbl�ttriger Storchschnabel|Rundbl�ttrige Storchschnabel]], den man in Mitteleuropa gelegentlich in [[Weinbau]]-Gebieten findet, ist vermutlich urspr�nglich im Mittelmeerraum beheimatet gewesen. Heute ist er nahezu weltweit verbreitet.\n\nIn einigen L�ndern haben die Storchschnabelarten so gute Ausgangsbedingungen gefunden, dass sie in sehr gro�em Ma�e verwildert sind und teilweise als Bioinvasoren angesehen werden. So wird das in Mitteleuropa beheimatete [[Ruprechtskraut]] an der Westk�ste der [[USA]] mittlerweile als unerw�nschtes Unkraut eingeordnet. Auch der [[Pyren�en-Storchschnabel]], den man in Mitteleuropa gelegentlich an Stra�enr�ndern findet, ist als sogenannter [[Neophyt]] zu betrachten. Anders als das in den USA ungern gesehene Ruprechtskraut fristet er in Mitteleuropa eher ein Nischendasein.\n\nZu den mitteleurop�ischen [[Arch�ophyt]]en geh�rt dagegen der [[Schlitzbl�ttriger Storchschnabel|Schlitzbl�ttrige Storchschnabel]]. Diese Storchschnabelart, die auf basen- und stickstoffsalzhaltigen Lehmboden w�chst, ist urspr�nglich im Mittelmeerraum beheimatet gewesen und z�hlt zu den [[Hemerochorie|hemerochoren]] Pflanzen, die mit den ersten Ackerbauern vermutlich �ber Saatgutverunreinigungen nach Mitteleuropa verschleppt wurden (sogenannte [[Speirochorie]]).\n\nDer [[Spreizender Storchschnabel|Spreizende Storchschnabel]] wird nur gelegentlich aus seinem Ursprungsgebiet, den warmen T�lern der West- und S�dalpen wie dem [[Kanton Wallis|Wallis]] und dem [[Veltlin]] nach Mitteleuropa in Form von Samen verschleppt (sogenannte [[Agochorie]]). Er ist dann in der Lage, sich vor�bergehend an dem neuen Standort zu etablieren. Er z�hlt daher zu den sogenannten [[Adventivpflanzen]].\n\n[[Datei:Illustration Geranium phaeum0.jpg|mini|Illustration: [[Brauner Storchschnabel]] (''{{lang|la|Geranium phaeum}}'')]]\n\n== Namensgebung, Beschreibung und �kologie ==\n\nDie deutsche Bezeichnung �Storchschnabel� erscheint beim ersten Blick auf die bl�hende Pflanze unverst�ndlich. Der Fruchtstand erkl�rt jedoch den Namen: Die l�nglichen, eigent�mlich gestalteten Fruchtst�nde erinnern an den [[Schnabel]] des Storches. Die botanische Bezeichnung ''Geranium'' basiert ebenfalls auf der Form der Fruchtst�nde; sie l�sst sich auf das griechische Wort \"g�ranos\" (Kranich) zur�ckf�hren. Im Deutschen wurde die Pflanze Storchschnabel fr�her<ref>Petrus Uffenbach (Hrsg.): ''Pedacii Dioscoridis Anazarbaei Kraeuterbuch ...'' (ins Deutsche �bersetzt von Johannes Danzius), Frankfurt am Main (bei Johann Bringern) 1610, S. 223.</ref> auch ''Kranichschnabel'' genannt.\n\n=== Die Pflanze ===\n\nStorchschnabel-Arten sind �berwiegend [[Ausdauernde Pflanze|ausdauernde]], seltener [[Einj�hrige Pflanze|ein-]] oder [[Zweij�hrige Pflanze|zweij�hrige]] [[krautige Pflanze]]n, wenige Arten sind [[Halbstrauch|Halbstr�ucher]] oder [[Strauch|Str�ucher]]. Sie enthalten [[�therische �le]].\nStorchschnabel-Arten wachsen buschig oder horstartig. In freier Natur sorgen die gro�en Bl�tter der Geranien und ihre h�ufig starke Breitenausdehnung daf�r, dass sie im Vergleich zu konkurrierenden Pflanzenarten an ihrem Standort verh�ltnism��ig viel N�hrstoffe und Wasser erhalten. Wie alle Familienmitglieder der Storchschnabelgew�chse haben Storchschnabel-Arten gelenkartig verbundene [[St�ngel]], die h�ufig [[Dr�senhaare]] haben. Einige Arten wie beispielsweise der [[Balkan-Storchschnabel]] sind nahezu immergr�n, andere wie der [[Basken-Storchschnabel]] bilden w�hrend ihrer Bl�tezeit gro�e, rundliche �Laubh�gel� aus, die w�hrend des Winterhalbjahrs verrotten.\n\n=== Die Bl�tter ===\n\nDie wechsel- oder gegenst�ndigen, gestielten [[Blatt (Pflanze)|Laubbl�tter]] sind je nach Art unterschiedlich gestaltet. Bei einigen Arten gleicht das Blatt der bei den Pelargonien-Arten vorkommenden runden Form, bei den meisten Arten ist es jedoch f�nfteilig und jeder Blattlappen stark eingekerbt. Stark geteilte Laubbl�tter hat beispielsweise ''Geranium purpureum''; bei dieser Art ist jedes Blatt in f�nf Lappen unterteilt, die Teilung reicht dabei bis zur Blattachse. Zus�tzlich ist jedes Blatt an der Spitze gelappt. Diese Blattform, die f�r viele der ''Geranium''-Arten typisch ist, bezeichnet man botanisch als tief fiederspaltig.\n\nBei den meisten Arten sind die Laubbl�tter einfarbig dunkelgr�n, bei nur wenigen Arten treten unterschiedliche Gr�nt�ne in der Blattfarbe auf. Die dunkelsten Laubbl�tter hat die auf [[Neuseeland]] und [[Tasmanien]] beheimatete Art ''Geranium sessiliflorum''. Bei einigen Sorten dieser Art wurde die ungew�hnliche Blattf�rbung noch vertieft, sie ist fast dunkelviolett.\n\n[[Nebenblatt|Nebenbl�tter]] sind vorhanden.\n\n[[Datei:Geranium pratense (Meadow Cranesbill).jpg|mini|Zuchtsorte des [[Wiesen-Storchschnabel]]s mit blauen Bl�tenkronbl�ttern]]\n\n=== Die Bl�ten ===\n\nDie Bl�ten stehen selten einzeln, meist zu zweit. Es ist in der Regel ein langer Bl�tenstiel vorhanden. Dies erm�glicht den Geranien an ihren nat�rlichen Standorten eine Konkurrenz zu den meist anderen, gleich hoch wachsenden Pflanzenarten von denen sie umgeben sind und auf diese Weise ihre [[Best�ubung]] sicherstellen.\n\nDie zwittrigen [[Bl�te]]n sind [[radi�rsymmetrisch]] und f�nfz�hlig mit doppelter [[Bl�tenh�lle]]. Die f�nf gr�nen, freien und h�ufig behaarten [[Kelchblatt|Kelchbl�tter]] weisen stets eine vorspringende Spitze auf. Die Kelchbl�tter schlie�en zuerst die Bl�tenknospe ein. Wenn sich nach der Best�ubung aus der Bl�te die Frucht entwickelt, vergr��ern sich die Kelchbl�tter und sch�tzen den Ansatz der entstehenden gro�en Frucht. Die f�nf freien [[Kronblatt|Kronbl�tter]] sind bei manchen Arten genagelt. Die Farbe der Bl�tenkronbl�tter der Storchschnabelarten reicht von Wei� �ber Rosa und Purpurrot bis zu einem leuchtenden Blau. Bei vielen Arten und Sorten ist eine deutliche Maserung der Kronbl�tter erkennbar.\nEs sind zwei Kreise mit je f�nf [[Staubblatt|Staubbl�ttern]] vorhanden, sie sind alle [[fertil]]; bei den anderen Gattungen der Familie ist ein Teil der Staubbl�tter zu [[Staminodien]] reduziert. Die R�nder der [[Staubf�den]] sind behaart. Die meist f�nf [[Nektarien]] des [[Diskus]] alternieren mit den Kronbl�ttern, selten sind sie zu einem Ring vereinigt. F�nf [[Fruchtblatt|Fruchtbl�tter]] sind zu einem oberst�ndigen [[Fruchtknoten]] verwachsen. Der Griffel endet in f�nf Narben.\n\n[[Datei:Geranium pratense flowerdiagram.png|mini|links|160px|[[Bl�tendiagramm]] von ''[[Geranium pratense]]'']]\nDie Bl�tenformel lautet: <math>\\star K_{5} \\; C_5 \\; A_{5+5} \\; G_{\\underline{(5)}}</math>\n\nJedes einzelne Bl�tenkronblatt ist im Gegensatz zum Kelchblatt bei der �berwiegenden Zahl der Arten am Ende abgerundet. Die Bl�tenform dagegen kann je nach Art")]
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
                Eukaryota,Eukaryoten,Dom�ne
                ,Lebewesen,Klassifikation
                """,
                """
                {{Taxobox
                | Taxon_Name       = Eukaryoten
                | Taxon_WissName   = Eukaryota
                | Taxon_Rang       = Dom�ne
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
                [[Datei:Binturong in Overloon.jpg|mini|Der [[Binturong]] z�hlt zu den Schleichkatzen]]

                dirt [[Datei:Binturong2.jpg|mini|Der [[Binturong]]
                z�hlt immer noch zu den Schleichkatzen]]
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
