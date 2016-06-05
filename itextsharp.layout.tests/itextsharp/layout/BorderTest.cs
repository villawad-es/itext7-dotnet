using System;
using System.IO;
using iTextSharp.Kernel.Color;
using iTextSharp.Kernel.Pdf;
using iTextSharp.Kernel.Utils;
using iTextSharp.Layout.Border;
using iTextSharp.Layout.Element;
using iTextSharp.Test;

namespace iTextSharp.Layout
{
	public class BorderTest : ExtendedITextTest
	{
		public static readonly String sourceFolder = NUnit.Framework.TestContext.CurrentContext
			.TestDirectory + "/../../resources/itextsharp/layout/BorderTest/";

		public static readonly String destinationFolder = NUnit.Framework.TestContext.CurrentContext
			.TestDirectory + "/test/itextsharp/layout/BorderTest/";

		public const String cmpPrefix = "cmp_";

		internal String fileName;

		internal String outFileName;

		internal String cmpFileName;

		[NUnit.Framework.TestFixtureSetUp]
		public static void BeforeClass()
		{
			CreateDestinationFolder(destinationFolder);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void SimpleBordersTest()
		{
			fileName = "simpleBordersTest.pdf";
			Document doc = CreateDocument();
			List list = new List();
			ListItem solidBorderItem = new ListItem("solid");
			solidBorderItem.SetBorder(new SolidBorder(iTextSharp.Kernel.Color.Color.RED, 6)).
				SetMarginBottom(5);
			solidBorderItem.SetBorderTop(new SolidBorder(iTextSharp.Kernel.Color.Color.BLUE, 
				10));
			list.Add(solidBorderItem);
			ListItem doubleBorderItem = new ListItem("double");
			doubleBorderItem.SetBorder(new DoubleBorder(iTextSharp.Kernel.Color.Color.RED, 10
				)).SetMarginBottom(5);
			doubleBorderItem.SetBorderRight(new DoubleBorder(iTextSharp.Kernel.Color.Color.BLUE
				, 6));
			list.Add(doubleBorderItem);
			ListItem dashedBorderItem = new ListItem("dashed");
			dashedBorderItem.SetBorder(new DashedBorder(iTextSharp.Kernel.Color.Color.GRAY, 2
				)).SetMarginBottom(5);
			dashedBorderItem.SetBorderBottom(new DashedBorder(iTextSharp.Kernel.Color.Color.BLACK
				, 4));
			list.Add(dashedBorderItem);
			ListItem dottedBorderItem = new ListItem("dotted");
			dottedBorderItem.SetBorder(new DottedBorder(iTextSharp.Kernel.Color.Color.BLACK, 
				3)).SetMarginBottom(5);
			dottedBorderItem.SetBorderLeft(new DottedBorder(iTextSharp.Kernel.Color.Color.GRAY
				, 6));
			list.Add(dottedBorderItem);
			ListItem roundDotsBorderItem = new ListItem("round dots");
			roundDotsBorderItem.SetBorder(new RoundDotsBorder(iTextSharp.Kernel.Color.Color.LIGHT_GRAY
				, 3)).SetMarginBottom(5);
			roundDotsBorderItem.SetBorderLeft(new RoundDotsBorder(iTextSharp.Kernel.Color.Color
				.BLUE, 5));
			list.Add(roundDotsBorderItem);
			doc.Add(list);
			CloseDocumentAndCompareOutputs(doc);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void Borders3DTest()
		{
			fileName = "borders3DTest.pdf";
			Document doc = CreateDocument();
			List list = new List();
			ListItem grooveBorderItem = new ListItem("groove");
			grooveBorderItem.SetBorder(new GrooveBorder(2)).SetMarginBottom(5).SetWidth(100);
			list.Add(grooveBorderItem);
			ListItem ridgeBorderItem = new ListItem("ridge");
			ridgeBorderItem.SetBorder(new RidgeBorder(2)).SetMarginBottom(5).SetWidth(100);
			list.Add(ridgeBorderItem);
			ListItem insetBorderItem = new ListItem("inset");
			insetBorderItem.SetBorder(new InsetBorder(1)).SetMarginBottom(5).SetWidth(100);
			list.Add(insetBorderItem);
			ListItem outsetBorderItem = new ListItem("outset");
			outsetBorderItem.SetBorder(new OutsetBorder(1)).SetMarginBottom(5).SetWidth(100);
			list.Add(outsetBorderItem);
			doc.Add(list);
			Paragraph emptyParagraph = new Paragraph("\n");
			doc.Add(emptyParagraph);
			DeviceRgb blueRgb = new DeviceRgb(0, 0, 200);
			DeviceRgb greenRgb = new DeviceRgb(0, 255, 0);
			DeviceCmyk magentaCmyk = new DeviceCmyk(0, 100, 0, 0);
			DeviceCmyk yellowCmyk = new DeviceCmyk(0, 0, 100, 0);
			list = new List();
			grooveBorderItem = new ListItem("groove");
			grooveBorderItem.SetBorder(new GrooveBorder(blueRgb, 2)).SetMarginBottom(5).SetWidth
				(100);
			list.Add(grooveBorderItem);
			ridgeBorderItem = new ListItem("ridge");
			ridgeBorderItem.SetBorder(new RidgeBorder(greenRgb, 2)).SetMarginBottom(5).SetWidth
				(100);
			list.Add(ridgeBorderItem);
			insetBorderItem = new ListItem("inset");
			insetBorderItem.SetBorder(new InsetBorder(magentaCmyk, 1)).SetMarginBottom(5).SetWidth
				(100);
			list.Add(insetBorderItem);
			outsetBorderItem = new ListItem("outset");
			outsetBorderItem.SetBorder(new OutsetBorder(yellowCmyk, 1)).SetMarginBottom(5).SetWidth
				(100);
			list.Add(outsetBorderItem);
			doc.Add(list);
			emptyParagraph = new Paragraph("\n");
			doc.Add(emptyParagraph);
			list = new List();
			grooveBorderItem = new ListItem("groove");
			grooveBorderItem.SetBorder(new GrooveBorder(yellowCmyk, 8)).SetMarginBottom(5);
			list.Add(grooveBorderItem);
			ridgeBorderItem = new ListItem("ridge");
			ridgeBorderItem.SetBorder(new RidgeBorder(magentaCmyk, 8)).SetMarginBottom(5);
			list.Add(ridgeBorderItem);
			insetBorderItem = new ListItem("inset");
			insetBorderItem.SetBorder(new InsetBorder(greenRgb, 8)).SetMarginBottom(5);
			list.Add(insetBorderItem);
			outsetBorderItem = new ListItem("outset");
			outsetBorderItem.SetBorder(new OutsetBorder(blueRgb, 8)).SetMarginBottom(5);
			list.Add(outsetBorderItem);
			doc.Add(list);
			CloseDocumentAndCompareOutputs(doc);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void BorderSidesTest()
		{
			fileName = "borderSidesTest.pdf";
			Document doc = CreateDocument();
			String text = "<p class=\"none\"  >No border.</p>\n" + "<p class=\"dotted\">A dotted border.</p>\n"
				 + "<p class=\"dashed\">A dashed border.</p>\n" + "<p class=\"solid\" >A solid border.</p>\n"
				 + "<p class=\"double\">A double border.</p>\n" + "<p class=\"groove\">A groove border.</p>\n"
				 + "<p class=\"ridge\" >A ridge border.</p>\n" + "<p class=\"inset\" >An inset border.</p>\n"
				 + "<p class=\"outset\">An outset border.</p>\n" + "<p class=\"hidden\">A hidden border.</p>";
			Paragraph p = new Paragraph(text);
			p.SetBorderTop(new SolidBorder(DeviceCmyk.MAGENTA, 4));
			p.SetBorderRight(new DoubleBorder(DeviceRgb.RED, 6));
			p.SetBorderBottom(new RoundDotsBorder(DeviceCmyk.CYAN, 2));
			p.SetBorderLeft(new DashedBorder(DeviceGray.BLACK, 3));
			doc.Add(p);
			doc.Add(new Paragraph(text).SetBorderTop(new SolidBorder(DeviceCmyk.MAGENTA, 8)));
			doc.Add(new Paragraph(text).SetBorderRight(new DoubleBorder(DeviceRgb.RED, 4)));
			doc.Add(new Paragraph(text).SetBorderBottom(new RoundDotsBorder(DeviceCmyk.CYAN, 
				3)));
			doc.Add(new Paragraph(text).SetBorderLeft(new DashedBorder(DeviceGray.BLACK, 5)));
			doc.Add(new Paragraph(text).SetBorder(new DottedBorder(DeviceGray.BLACK, 1)));
			CloseDocumentAndCompareOutputs(doc);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void BorderBoxTest()
		{
			fileName = "borderBoxTest.pdf";
			Document doc = CreateDocument();
			String textBefore = "At the mid-oceanic ridges, two tectonic plates diverge from one another as new oceanic crust is formed by the cooling and "
				 + "solidifying of hot molten rock. Because the crust is very thin at these ridges due to the pull of the tectonic plates, the release of "
				 + "pressure leads to adiabatic expansion and the partial melting of the mantle, causing volcanism and creating new oceanic crust. Most divergent "
				 + "plate boundaries are at the bottom of the oceans; therefore, most volcanic activity is submarine, forming new seafloor. Black smokers (also "
				 + "known as deep sea vents) are an example of this kind of volcanic activity. Where the mid-oceanic ridge is above sea-level, volcanic islands are "
				 + "formed, for example, Iceland.";
			String text = "Earth's volcanoes occur because its crust is broken into 17 major, rigid tectonic plates that float on a hotter,"
				 + " softer layer in its mantle. Therefore, on Earth, volcanoes are generally found where tectonic plates are diverging or converging. "
				 + "For example, a mid-oceanic ridge, such as the Mid-Atlantic Ridge, has volcanoes caused by divergent tectonic plates pulling apart;"
				 + " the Pacific Ring of Fire has volcanoes caused by convergent tectonic plates coming together. Volcanoes can also form where there is "
				 + "stretching and thinning of the crust's interior plates, e.g., in the East African Rift and the Wells Gray-Clearwater volcanic field and "
				 + "Rio Grande Rift in North America. This type of volcanism falls under the umbrella of \"plate hypothesis\" volcanism. Volcanism away "
				 + "from plate boundaries has also been explained as mantle plumes. These so-called \"hotspots\", for example Hawaii, are postulated to arise "
				 + "from upwelling diapirs with magma from the core-mantle boundary, 3,000 km deep in the Earth. Volcanoes are usually not created where two "
				 + "tectonic plates slide past one another.";
			String textAfter = "Subduction zones are places where two plates, usually an oceanic plate and a continental plate, collide. In this case, the oceanic "
				 + "plate subducts, or submerges under the continental plate forming a deep ocean trench just offshore. In a process called flux melting, water released"
				 + " from the subducting plate lowers the melting temperature of the overlying mantle wedge, creating magma. This magma tends to be very viscous due to "
				 + "its high silica content, so often does not reach the surface and cools at depth. When it does reach the surface, a volcano is formed. Typical examples"
				 + " of this kind of volcano are Mount Etna and the volcanoes in the Pacific Ring of Fire.";
			doc.Add(new Paragraph(textBefore).SetMargins(25, 60, 70, 80));
			Paragraph p = new Paragraph(text).SetBackgroundColor(iTextSharp.Kernel.Color.Color
				.GRAY);
			p.SetMargins(25, 60, 70, 80);
			p.SetBorderLeft(new DoubleBorder(DeviceRgb.RED, 25));
			p.SetBorder(new DoubleBorder(DeviceRgb.BLACK, 6));
			doc.Add(p);
			doc.Add(new Paragraph(textAfter).SetBorder(new DottedBorder(iTextSharp.Kernel.Color.Color
				.BLACK, 3)).SetBorderRight(new DottedBorder(iTextSharp.Kernel.Color.Color.BLACK, 
				12)));
			CloseDocumentAndCompareOutputs(doc);
		}

		/// <exception cref="System.IO.FileNotFoundException"/>
		private Document CreateDocument()
		{
			outFileName = destinationFolder + fileName;
			cmpFileName = sourceFolder + cmpPrefix + fileName;
			PdfDocument pdfDocument = new PdfDocument(new PdfWriter(new FileStream(outFileName
				, FileMode.Create)));
			return new Document(pdfDocument);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Exception"/>
		private void CloseDocumentAndCompareOutputs(Document document)
		{
			document.Close();
			String compareResult = new CompareTool().CompareByContent(outFileName, cmpFileName
				, destinationFolder, "diff");
			if (compareResult != null)
			{
				NUnit.Framework.Assert.Fail(compareResult);
			}
		}
	}
}
