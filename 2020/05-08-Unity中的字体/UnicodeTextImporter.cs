using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text;
// definition used in thie class
enum FontStyle
{
	FONTSTYLE_NONE          = 0x00, // 0000 0000
	FONTSTYLE_BOLD          = 0x01, // 0000 0001
	FONTSTYLE_ITALIC        = 0x02, // 0000 0010
	FONTSTYLE_STRIKEOUT     = 0x04, // 0000 0100
	FONTSTYLE_UNDERLINE     = 0x08, // 0000 1000
	FONTSTYLE_ANTIALIASED   = 0x10, // 0001 0000
};

//class UnicodeConfigInfo
class UnicodeConfigInfo
{
	UnicodeConfigInfo()
	{
		this.m_version = 1;
		this.m_fontSize = 12;
		this.m_fontName = "";
		this.m_fontStyle = (short)FontStyle.FONTSTYLE_NONE;
	}

	public int m_version
	{
		get { return m_version; }
		set { m_version = value; }
	}
	public int m_fontSize { get; set; }
	public short m_fontStyle { get; set; }
	public string m_fontName { get; set; }
	
};

//class wcharData
public class WcharData
{
	public WcharData(int p, char c)
	{
		this.m_priority = p;
		this.m_char = c;
	}

	public int m_priority { get; set; }   // the number of usages
	public char m_char { get; set; }      // Unicode encoding value
};

public sealed class UnicodeTextImporter
{
	//data
	ArrayList m_sortedData = new ArrayList();
	
	// This is a singleton!
	private UnicodeTextImporter() {}
	private static readonly UnicodeTextImporter m_instance = new UnicodeTextImporter();
	
	public static UnicodeTextImporter Instance
	{
		get { return m_instance; }
	}

	// Use this for initialization
	void Start()
	{
	}
	
	//Reset this class data
	void Reset()
	{
		int count = m_sortedData.Count;
    	for( int i = 0; i < count; i++ )
    	{
        	m_sortedData[i] = null;
    	}
    	m_sortedData.Clear();
		//GC
		System.GC.Collect();
	}
	[MenuItem ("AssetDatabase/Import Font with Text File")]
    static void ImportFontWithTextFromFile()
	{
		// A font has to be selected in Project view
		Font theFont = Selection.activeObject as Font;
		if( theFont == null )
		{
			Debug.LogWarning( "A font has to be selected in Project view." );
			return;
		}
		//open file dialog
		string path = EditorUtility.OpenFilePanel(
			"Open Unicode Text File", Application.dataPath + "/Resources", "txt");
		
		//Debug info
		Debug.Log( "ImportFontWithTextFromFile: text filePath=" + path );
		
		if( string.IsNullOrEmpty(path) )
		{
			Debug.LogError( "Doesn't find Unicode text file!" );
		}
		//Load from the file
		Instance.LoadUnicodeTextFromFile(path);
		
		//For debugging: display the sorted unicode text
		//Instance.DisplaySortedWords();
		string str = Instance.GetUnicodeString();
		Debug.Log( "ImportFontWithTextFromFile: Final Unicode String: " + str );
		//now, import font again with the selected font in the Assets
		
		// Get the Importer
		string fontPath = AssetDatabase.GetAssetPath(theFont);
		TrueTypeFontImporter fontImporter = AssetImporter.GetAtPath(fontPath) as TrueTypeFontImporter;
		//setup font importer
		fontImporter.fontSize = 16;
		//fontImporter.use2xBehaviour = false;
		//fontImporter.fontTTFName = theFont.name;
		fontImporter.fontTextureCase = FontTextureCase.CustomSet;
		fontImporter.customCharacters = str;
		//start import
		AssetDatabase.ImportAsset(fontPath);	
	}
	void LoadUnicodeTextFromFile(string filePath)
	{
		//Load file as Unicode string
		StreamReader reader = new StreamReader(filePath, Encoding.Unicode);
    	string fileContents = reader.ReadToEnd();
    	reader.Close();	
		//process text data
		ProcessUnicodeText( fileContents );	
		//Add default chars
		AddDefaultChars();	
		//Sort the words: most used words will have higher priority
		//SortUniqueData();
		QuickSort( 0, m_sortedData.Count-1 );		
	}	
	void ProcessUnicodeText( string fileContents )
	{
		string[] lines = fileContents.Split( "\n"[0] );
		Debug.Log( "ProcessUnicodeText: lines of text=" + lines.Length );
		for( int i = 0; i < lines.Length; i++ )
		{
			string words = lines[i];
			
			//DB
			Debug.Log( "\t line" + i.ToString() + "(" + words.Length + "): " + words );
			
			for( int j = 0; j < words.Length; j++ )
			{
				char word = words[j];
				//Debug.Log( "\t\t word" + j.ToString() + ": " + word );
				SetCharacterPriority( word );
			}
		}
	}
	void SetCharacterPriority( char word )
	{
	    int sortedDataCount = (int)m_sortedData.Count;	
	    // If the list is empty, just create a wcharData and add it into list.
	    if( sortedDataCount == 0 )
	    {
	        WcharData pData = new WcharData(1, word);
	        m_sortedData.Add(pData);
	        return;
	    }	
	    // Otherwise, try to find it.
	    int iFoundIndex = -1;
	    for( int i = 0; i < sortedDataCount; i++ )
	    {
	        WcharData pData = m_sortedData[i] as WcharData;
	        if( pData.m_char == word )
	        {
	            iFoundIndex = i;
	            break;
	        }
	    }
	    if( iFoundIndex != -1 )
	    {
	        // The char exists, increase the priority vlaue.
			WcharData pData = m_sortedData[iFoundIndex] as WcharData;
	        pData.m_priority += 1;
	    }
	    else
	    {
	        WcharData pData = new WcharData(1, word);
	        m_sortedData.Add(pData);
	    }
	}
	//sort the data - slow function
	void SortUniqueData()
	{
	    int sortedDataCount = (int)m_sortedData.Count;
	    for( int i = 0; i < sortedDataCount - 1; i++ )
	    {
	        WcharData pData0 = m_sortedData[i] as WcharData;
	        for( int j = i + 1; j < sortedDataCount; j++)
	        {
	            WcharData pData1 = m_sortedData[j] as WcharData;
	            if( pData0.m_priority < pData1.m_priority )
	            {
	                // Swap
	                m_sortedData[i] = pData1;
	                m_sortedData[j] = pData0;
	                pData0 = pData1;
	            }
	        }
	    }
	}	
	//Quick sort
	int CompareFunc(WcharData data1, WcharData data2)
	{
		if( data1.m_priority == data2.m_priority )
		{
			return 0;
		}
		else
		{
			if( data1.m_priority >= data2.m_priority )
			{
				return -1;
			}
			else
			{
				return 1;
			}

		}
	}	
	void QuickSort(int left, int right)  
	{  
		int i, j;
		WcharData x, y;
  
		i = left;
		j = right;
		x = m_sortedData[(left + right) / 2] as WcharData;  
		do
		{  
			while( (((WcharData)m_sortedData[i]).m_priority > x.m_priority) && (i < right) ) i++;  
			while( (x.m_priority > ((WcharData)m_sortedData[j]).m_priority) && (j > left) ) j--;  
  
			if( i <= j )
			{  
				y = m_sortedData[i] as WcharData;  
				m_sortedData[i] = m_sortedData[j];  
				m_sortedData[j] = y;  
				i++;
				j--;  
			}  
		}
		while( i <= j );  
  
		if( left < j )
		{
			QuickSort( left, j );
		}
		if( i < right )
		{
			QuickSort( i, right );
		}
	} 
	void AddDefaultChars()
	{
		short theChar;
        for( theChar = 32; theChar <= 126; theChar++ )
        {
            // If the character doesn't allready exists then add it
            if( !IsCharExists( (char)theChar) )
            {
                WcharData pData = new WcharData(1, (char)theChar);
                m_sortedData.Add(pData);
            }
        }
	}	
	//check if a given char exists in the m_sortedData list
	bool IsCharExists( char theChar )
	{
		int count = m_sortedData.Count;
	    for( int i = 0; i < count; i++ )
	    {
	        WcharData data = m_sortedData[i] as WcharData;
	        if( (data != null) && (data.m_char == theChar) )
	            return true;
	    }
	
	    return false;
	}	
	//For debugging
	void DisplaySortedWords()
	{
		int count = m_sortedData.Count;
		Debug.Log( "Total words = " + count.ToString() );
	    for( int i = 0; i < count; i++ )
	    {
	        WcharData data = m_sortedData[i] as WcharData;
	        Debug.Log( "\t  Word" + i.ToString() + "[" + data.m_char + " " + data.m_priority.ToString() + "]" );
	    }
	}
	string GetUnicodeString()
	{
		int count = m_sortedData.Count;
		char[] wordList = new char[count];
		for( int i = 0; i < count; i++ )
	    {
	        WcharData data = m_sortedData[i] as WcharData;
			wordList[i] = data.m_char;
		}
		
		string str = new string(wordList);
		return str;
	}
}
