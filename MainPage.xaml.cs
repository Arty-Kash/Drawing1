using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

using Xamarin.Forms;

using SkiaSharp;
using SkiaSharp.Views.Forms;

using TouchTracking;

namespace DrawTouch
{
    public partial class MainPage : ContentPage
    {
        public class Page
        {
            public string Title { get; set; }
            public string Characters { get; set; }
        }

        public static ObservableCollection<Page> Pages;

        public class Link
        {
            public bool exist;			// true, if link exists
            public int x1, y1, x2, y2;	// start and end points
        }

        Dictionary<ContentView, DragInfo> dragDictionary = new Dictionary<ContentView, DragInfo>();

		// set some array variables to memory the corresponding variable
		static int maxpage = 20;
		static int maxnode = 10;
        ContentPage[] contentPages = new ContentPage[maxpage];
        SKCanvasView[] canvasViews = new SKCanvasView[maxpage];
        AbsoluteLayout[] absoluteLayouts = new AbsoluteLayout[maxpage];
		ContentView[,] node = new ContentView[maxpage, maxnode];
		Link[,,] link = new Link[maxpage, maxnode, maxnode];
		string[,,] linkTitle = new string[maxpage, maxnode, maxnode];
		
		int page = 0;   // Current Page
        int NoP = 0;    // The Number of Page
        int[] NoN = Enumerable.Repeat<int>(0, maxnode).ToArray(); //The Number of Nodes in each Page

		int linkStart, linkEnd = 0;	// temporary variable
		string entryText = "";		// temporary variable
		int nodeID = 0;             // temporary variable

		int scale = 2;  // canvas scale:  2(iOS),  1(UWP)
		int space = 7;  // space between two arrows with opposite direction
		

		public MainPage()
		{
			InitializeComponent();

			// Initialize PageListView
			Pages = new ObservableCollection<Page>()
			{
				new Page(){Title="No Groups", Characters="" }
			};
			PageListView.ItemsSource = Pages;

			// Initialize link[,,]
			for (int i = 0; i < 20; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					for (int k = 0; k < 10; k++)
					{
						link[i, j, k] = new Link { exist = false, x1 = 0, y1 = 0, x2 = 0, y2 = 0 };
					}
				}
			}
		}


		// Add Character Group Title to PageListView, and 
		// Create a new Character Group Page to draw the characters correlation diagram
		void AddPage(object sender, EventArgs args)
		{
			// Add Character Group Title to PageListView
			Page new_page = new Page()
			{
				Title = CharaGroupTitle.Text,
				Characters = Characters.Text
			};
			if (NoP == 0) Pages.Clear(); // Delete "No Pages" message, if 1st item was added 
			Pages.Add(new_page);


            // stacklayoutTop1
            Entry AddNodeEntry = new Entry
            {
                Placeholder = "Character Name", 
                WidthRequest=230
            };
            AddNodeEntry.TextChanged += AddNodeEntryTextChanged;
            Button AddNodeButton = new Button
            {
                Text = "Add Character",
                BorderColor = Color.Blue,
                BorderWidth = 1, 
                HorizontalOptions = LayoutOptions.EndAndExpand, 
                WidthRequest=120
            };
			AddNodeButton.Clicked += AddNode;
            StackLayout stacklayoutTop1 = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children =
                {
                    AddNodeEntry, 
                    AddNodeButton
                }
            };

            // stacklayoutTop2
            Entry AddLinkEntry = new Entry
            {
                Placeholder = "Link Name",
                WidthRequest = 230
            };
            AddLinkEntry.TextChanged += AddLinkEntryTextChanged;
			Button AddLinkButton = new Button
			{
                Text = "Add Link", 
                BorderColor = Color.Blue,
                BorderWidth = 1, 
                HorizontalOptions = LayoutOptions.EndAndExpand, 
                WidthRequest=120
			};
			AddLinkButton.Clicked += AddLink;
			StackLayout stacklayoutTop2 = new StackLayout
			{
				Orientation=StackOrientation.Horizontal, 
				Children =
                {
                    AddLinkEntry,
					AddLinkButton
				}
			};

			// Character Group Title
			Label charagroupTitle = new Label
			{
				Text = CharaGroupTitle.Text,
				FontSize = 25
			};


			// create SkiaSharp canvas and AbsoluteLayout to draw CharacterNode
			SKCanvasView canvas = new SKCanvasView();
			canvas.PaintSurface += OnCanvasViewPaintSurface;

			AbsoluteLayout absoluteLayout = new AbsoluteLayout	{ };

			Grid gridCanvas = new Grid
			{
				HeightRequest = 500, 
				WidthRequest = 300,
				BackgroundColor = Color.Gray,
				Children = {
					canvas, 
					absoluteLayout
				}
			};

			//stacklayoutBottom
			Button PreviousPage = new Button
			{
				Text="<=", 
				FontSize=20, 
				HorizontalOptions=LayoutOptions.Start
			};
			PreviousPage.Clicked += MovePreviousPage;
			Label LabelPageNo = new Label
			{
				Text = "Page " + (NoP + 1).ToString(),
				FontSize = 20, 
				HorizontalOptions=LayoutOptions.CenterAndExpand
			};
			Button TopPage = new Button
			{
				Text = "Top Page",
			};
			TopPage.Clicked += PopToRoot;
			Button NextPage = new Button
			{
				Text = "=>", 
				FontSize=20,
				HorizontalOptions = LayoutOptions.EndAndExpand
			};
			NextPage.Clicked += MoveNextPage;
			StackLayout stacklayoutBottom = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				Children = {
					PreviousPage,
					TopPage,
					LabelPageNo, 
					NextPage
				}
			};

			ContentPage contentPage = new ContentPage
			{
				Content = new StackLayout
				{
					Children =
					{
                        stacklayoutTop1, 
						stacklayoutTop2,
						charagroupTitle,
						gridCanvas,						
						stacklayoutBottom
					}
				}
			};

			// store the value of the current contentPage, absoluteLayout and canvas
			contentPages[NoP] = contentPage;
			absoluteLayouts[NoP] = absoluteLayout;
			canvasViews[NoP] = canvas;
			NoP++;
			CharaGroupTitle.Text = ""; Characters.Text = "";  // Clear text input in Entry
		}


        // Get the page number of the tapped item and jump to the page
        async void JumpPage(object sender, ItemTappedEventArgs e)
        {
            var index = Pages.IndexOf((Page)e.Item);
            await Navigation.PushAsync(contentPages[index]);

            page = (int)index;  // set Current Page Number
        }


        // Add Character Node
        Entry entry = new Entry();
        void AddNodeEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            entry = sender as Entry;
            entryText = entry.Text;
        }
		void AddNode(object sender, EventArgs args)
		{
			AddNodeBox(absoluteLayouts[page]);

			entry.Text = "";
			NoN[page]++;
		}

        // set flag to control node selection for linking
        void AddLinkEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            entry = sender as Entry;
            entryText = entry.Text;
        }
        int AddLinkFlag = 0;	// Initialize AddArrowFlag
        void AddLink(object sender, EventArgs args)
        {
            AddLinkFlag = 1;	// 1st node has been selected

			// Initialize Node Boarder Color (Red -> Black) 
			for (int i = 0; i < NoN[page]; i++)
            {
                node[page, i].BackgroundColor = Color.Black;
            }
        }


		// Page Control
		async void MovePreviousPage(object sender, EventArgs args)
		{
			if (page == 0)
			{
				await Navigation.PopToRootAsync();
				return;
			}
			page--;
			await Navigation.PopAsync();
		}
		async void MoveNextPage(object sender, EventArgs args)
		{
			if (page == (NoP-1))
			{
				await DisplayAlert("",page.ToString(), "OK");
				return;
			}
			page++;
			await Navigation.PushAsync(contentPages[page]);
		}
		async void PopToRoot(object sender, EventArgs args)
		{
			await Navigation.PopToRootAsync();
		}


		// add Character Node
		void AddNodeBox(AbsoluteLayout absLayout)
		{
            Entry CharacterName = new Entry
            {
                FontSize = 20,
                BackgroundColor = Color.White,
                InputTransparent = true, 
                Text=entryText
			};

			ContentView contentView = new ContentView
			{
				BackgroundColor = Color.Black, 
				Padding = new Thickness(3),
				Content=new StackLayout {
                    Children = { CharacterName }
				}
			};


			node[page, NoN[page]] = contentView;

			// detect tap event
			var tap = new TapGestureRecognizer();
			tap.Tapped += (s, e) =>
			{
				if(AddLinkFlag==1)		// 1st node has already been selected
				{
					AddLinkFlag = 2;	// 2nd node has been selected
					ContentView view = contentView as ContentView;
					Rectangle rect = AbsoluteLayout.GetLayoutBounds(view);

                    for (int i = 0; i < NoN[page]; i++){
                        if (view == node[page, i]){
                            linkStart = i; break;
                        } 
                    }

					view.BackgroundColor = Color.Red;
					return;
				}
				else if(AddLinkFlag==2)	// 2nd node has already been selected
				{
					AddLinkFlag = 0;	//reset AddLinkFlag
					ContentView view = contentView as ContentView;
					Rectangle rect = AbsoluteLayout.GetLayoutBounds(view);

					for (int i = 0; i < NoN[page]; i++)
                    {
                        if (view == node[page, i]) {
                            linkEnd = i; break;
                        }
                    }

					// set initial link position to the center of the each node
					link[page, linkStart, linkEnd].exist = true;
					link[page, linkStart, linkEnd].x1 = (int)((node[page,linkStart].X + node[page,linkStart].Width/2) * scale);
					link[page, linkStart, linkEnd].y1 = (int)((node[page,linkStart].Y + node[page,linkStart].Height/2) * scale);
					link[page, linkStart, linkEnd].x2 = (int)((node[page,linkEnd].X + node[page,linkEnd].Width/2) * scale);
                    link[page, linkStart, linkEnd].y2 = (int)((node[page,linkEnd].Y + node[page,linkEnd].Height/2) * scale);
					linkTitle[page, linkStart, linkEnd] = entryText;

					view.BackgroundColor = Color.Red;
					entry.Text = ""; // clear AddLinkEntry
					return;
				}
			};
			contentView.GestureRecognizers.Add(tap);

			TouchEffect touchEffect = new TouchEffect();
			touchEffect.TouchAction += OnTouchEffectAction;
			contentView.Effects.Add(touchEffect);
			absLayout.Children.Add(contentView);
		}


		void OnTouchEffectAction(object sender, TouchActionEventArgs args)
		{
			ContentView view = sender as ContentView;
			SKCanvasView canvas = new SKCanvasView();

			canvas = canvasViews[page];

            // identify the touched node
            for (int i = 0; i < NoN[page]; i++)
            {
                if (view == node[page, i]) { nodeID = i;   break; }
            }

			switch (args.Type)
			{
				case TouchActionType.Pressed:
					// Don't allow a second touch on an already touched BoxView
					if (!dragDictionary.ContainsKey(view))
					{
						dragDictionary.Add(view, new DragInfo(args.Id, args.Location));

						// Set Capture property to true
						TouchEffect touchEffect = (TouchEffect)view.Effects.FirstOrDefault(e => e is TouchEffect);
						touchEffect.Capture = true;
					}
					break;

				case TouchActionType.Moved:
					if (dragDictionary.ContainsKey(view) && dragDictionary[view].Id == args.Id)
					{
						// update all touched node posisions
						Rectangle rect = AbsoluteLayout.GetLayoutBounds(view);
						Point initialLocation = dragDictionary[view].PressPoint;
						rect.X += args.Location.X - initialLocation.X;
						rect.Y += args.Location.Y - initialLocation.Y;
						AbsoluteLayout.SetLayoutBounds(view, rect);

						// Update all link positions of the touched node
						// The positions depend on the positions of the start and end node
                        for (int i = 0; i < NoN[page]; i++){
                            if(link[page,nodeID,i].exist){
								if (node[page, nodeID].X + node[page, nodeID].Width < node[page, i].X)
								{
									link[page, nodeID, i].x1 = (int)((node[page, nodeID].X + node[page, nodeID].Width) * scale);
									link[page, nodeID, i].y1 = (int)((node[page, nodeID].Y + node[page, nodeID].Height / 2 + space) * scale);
									link[page, nodeID, i].x2 = (int)(node[page, i].X * scale);
									link[page, nodeID, i].y2 = (int)((node[page, i].Y + node[page, i].Height / 2 + space) * scale);
								}
								else if (node[page, i].X + node[page, i].Width < node[page, nodeID].X)
								{
									link[page, nodeID, i].x1 = (int)(node[page, nodeID].X * scale);
									link[page, nodeID, i].y1 = (int)((node[page, nodeID].Y + node[page, nodeID].Height / 2 + space) * scale);
									link[page, nodeID, i].x2 = (int)((node[page, i].X + node[page, i].Width) * scale);
									link[page, nodeID, i].y2 = (int)((node[page, i].Y + node[page, i].Height / 2 +space) * scale);
								}
								else if (node[page, nodeID].Y < node[page, i].Y + node[page, i].Height)
								{
									link[page, nodeID, i].x1 = (int)((node[page, nodeID].X + node[page, nodeID].Width / 2 + space) * scale);
									link[page, nodeID, i].y1 = (int)((node[page, nodeID].Y + node[page, nodeID].Height) * scale);
									link[page, nodeID, i].x2 = (int)((node[page, i].X + node[page, i].Width / 2 + space) * scale);
									link[page, nodeID, i].y2 = (int)((node[page, i].Y) * scale);
								}
								else
								{
									link[page, nodeID, i].x1 = (int)((node[page, nodeID].X + node[page, nodeID].Width / 2 + space) * scale);
									link[page, nodeID, i].y1 = (int)((node[page, nodeID].Y) * scale);
									link[page, nodeID, i].x2 = (int)((node[page, i].X + node[page, i].Width / 2 + space) * scale);
									link[page, nodeID, i].y2 = (int)((node[page, i].Y + node[page, i].Height) * scale);
								}

							}
                            if (link[page, i, nodeID].exist)
                            {
								if (node[page, i].X + node[page, i].Width < node[page, nodeID].X )
								{
									link[page, i, nodeID].x1 = (int)((node[page, i].X + node[page, i].Width) * scale);
									link[page, i, nodeID].y1 = (int)((node[page, i].Y + node[page, nodeID].Height / 2 - space) * scale);
									link[page, i, nodeID].x2 = (int)(node[page, nodeID].X * scale);
									link[page, i, nodeID].y2 = (int)((node[page, nodeID].Y + node[page, nodeID].Height / 2 -space) * scale);
								}
								else if(node[page, nodeID].X + node[page, nodeID].Width < node[page, i].X)
								{
									link[page, i, nodeID].x1 = (int)(node[page, i].X * scale);
									link[page, i, nodeID].y1 = (int)((node[page, i].Y + node[page, i].Height / 2 - space) * scale);
									link[page, i, nodeID].x2 = (int)((node[page, nodeID].X + node[page, nodeID].Width) * scale);
									link[page, i, nodeID].y2 = (int)((node[page, nodeID].Y + node[page, nodeID].Height / 2 - space) * scale);
								}
								else if(node[page, i].Y < node[page, nodeID].Y+node[page, nodeID].Height)
								{
									link[page, i, nodeID].x1 = (int)((node[page, i].X + node[page, i].Width / 2 - space) * scale);
									link[page, i, nodeID].y1 = (int)((node[page, i].Y + node[page, i].Height ) * scale);
									link[page, i, nodeID].x2 = (int)((node[page, nodeID].X + node[page, nodeID].Width / 2 -space) * scale);
									link[page, i, nodeID].y2 = (int)((node[page, nodeID].Y ) * scale);
								}
								else
								{
									link[page, i, nodeID].x1 = (int)((node[page, i].X + node[page, i].Width / 2 - space) * scale);
									link[page, i, nodeID].y1 = (int)((node[page, i].Y) * scale);
									link[page, i, nodeID].x2 = (int)((node[page, nodeID].X + node[page, nodeID].Width / 2 - space) * scale);
									link[page, i, nodeID].y2 = (int)((node[page, nodeID].Y + node[page, nodeID].Height ) * scale);
								}
							}
                        }
						canvas.InvalidateSurface();
					}
					break;

				case TouchActionType.Released:
					if (dragDictionary.ContainsKey(view) && dragDictionary[view].Id == args.Id)
					{
						dragDictionary.Remove(view);
						canvas.InvalidateSurface();
					}
					break;
			}
		}


        // draw links using SkiaSharp
        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var surface = e.Surface;
            var canvas = surface.Canvas;

            canvas.Clear();

            // Draw Line
            for (int i = 0; i < NoN[page]; i++)
            {
                for (int j = 0; j < NoN[page]; j++)
                {
                    if (link[page, i, j].exist)
                    {
                        canvas.DrawLine(link[page, i, j].x1,
                                        link[page, i, j].y1,
                                        link[page, i, j].x2,
                                        link[page, i, j].y2, linePaint);
                        // draw circle instead of arrow at the ending point of this link
                        canvas.DrawCircle(link[page, i, j].x2,
                                          link[page, i, j].y2,
                                          5 * scale,
                                          circlePaint);
                        // draw link title
                        canvas.DrawText(linkTitle[page, i, j],
                                        (link[page, i, j].x1 + link[page, i, j].x2) / 2,
                                        (link[page, i, j].y1 + link[page, i, j].y2) / 2,
                                        textPaint);
                    }
                }
            }
        }
        private SKPaint linePaint = new SKPaint
        {
            StrokeWidth = 2,
            IsAntialias = true,
            Color = SKColors.Black
        };
        private SKPaint circlePaint = new SKPaint
        {
            Color = SKColors.Black
        };
        private SKPaint textPaint = new SKPaint
        {
            TextSize = 15,
            Color = SKColors.Black
        };
	}
}
