#region "  � Copyright 2005 to Marcos Meli - http://www.marcosmeli.com.ar" 

// Errors, suggestions, contributions, send a mail to: marcosdotnet[at]yahoo.com.ar.

#endregion

#if NET_2_0

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace FileHelpers
{
	/// <include file='FileHelperEngine.docs.xml' path='doc/FileHelperEngine/*'/>
	public sealed class FileHelperEngine<T> : EngineBase
	{
		#region "  Constructor  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/FileHelperEngineCtrG/*'/>
		public FileHelperEngine() : base(typeof(T))
		{
		}

		#endregion

		#region "  ReadFile  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadFile/*'/>
		public T[] ReadFile(string fileName)
		{
			using (StreamReader fs = new StreamReader(fileName, mEncoding, true))
			{
                T[] tempRes;
				tempRes = ReadStream(fs);
				fs.Close();

				return tempRes;
			}

		}

		#endregion

		#region "  ReadStream  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadStream/*'/>
        public T[] ReadStream(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader", "The reader of the Stream can�t be null");

			ResetFields();
			mHeaderText = String.Empty;
            mFooterText = String.Empty;

			ArrayList resArray = new ArrayList();

            ForwardReader freader = new ForwardReader(reader, mRecordInfo.mIgnoreLast);
            freader.DiscardForward = true;

            string currentLine, completeLine;

			mLineNum = 1;

			completeLine = freader.ReadNextLine();
			currentLine = completeLine;

			if (mRecordInfo.mIgnoreFirst > 0)
			{
				for (int i = 0; i < mRecordInfo.mIgnoreFirst && currentLine != null; i++)
				{
					mHeaderText += currentLine + "\r\n";
					currentLine = freader.ReadNextLine();
					mLineNum++;
				}
			}


			bool byPass = false;

            while (currentLine != null)
			{
				try
				{
                    mTotalRecords++;
					resArray.Add(mRecordInfo.StringToRecord(currentLine));
				}
				catch (Exception ex)
				{
					switch (mErrorManager.ErrorMode)
					{
						case ErrorMode.ThrowException:
							byPass = true;
							throw;
						case ErrorMode.IgnoreAndContinue:
							break;
						case ErrorMode.SaveAndContinue:
							ErrorInfo err = new ErrorInfo();
							err.mLineNumber = mLineNum;
							err.mExceptionInfo = ex;
//							err.mColumnNumber = mColumnNum;
							err.mRecordString = completeLine;

							mErrorManager.AddError(err);
							break;
					}
				}
				finally
				{
					if (byPass == false)
					{
						currentLine = freader.ReadNextLine();
						completeLine = currentLine;
						mLineNum++;
					}
				}

			}

            if (mRecordInfo.mIgnoreLast > 0)
            {
                mFooterText = freader.RemainingText;
            }

            return (T[])resArray.ToArray(typeof(T));
		}

		#endregion

		#region "  ReadString  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadString/*'/>
        public T[] ReadString(string source)
		{
            StringReader reader = new StringReader(source);
            T[] res = ReadStream(reader);
            reader.Close();
            return res;
		}

		#endregion

		#region "  WriteFile  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteFile/*'/>
        public bool WriteFile(string fileName, T[] records)
		{
			return WriteFile(fileName, records, -1);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteFile2/*'/>
        public bool WriteFile(string fileName, T[] records, int maxRecords)
		{
			using (StreamWriter fs = new StreamWriter(fileName, false, mEncoding))
			{
				bool res;
				res = WriteStream(fs, records, maxRecords);
				fs.Close();
				return res;
			}

		}

		#endregion

		#region "  WriteStream  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteStream/*'/>
        public bool WriteStream(TextWriter writer, T[] records)
		{
			return WriteStream(writer, records, -1);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteStream2/*'/>
        public bool WriteStream(TextWriter writer, T[] records, int maxRecords)
		{
			if (writer == null)
				throw new ArgumentNullException("writer", "The writer of the Stream can be null");

			if (records == null)
				throw new ArgumentNullException("records", "The records can be null. Try with an empty array.");

			ResetFields();

			if (mHeaderText != null && mHeaderText.Length != 0)
				if (mHeaderText.EndsWith("\r\n"))
					writer.Write(mHeaderText);
				else
					writer.WriteLine(mHeaderText);


			string currentLine = null;

			//ConstructorInfo constr = mType.GetConstructor(new Type[] {});
			int max = records.Length;

			if (maxRecords >= 0)
				max = Math.Min(records.Length, maxRecords);


			for (int i = 0; i < max; i++)
			{
				try
				{
					currentLine = mRecordInfo.RecordToString(records[i]);
					writer.WriteLine(currentLine);
				}
				catch (Exception ex)
				{
					switch (mErrorManager.ErrorMode)
					{
						case ErrorMode.ThrowException:
							throw;
						case ErrorMode.IgnoreAndContinue:
							break;
						case ErrorMode.SaveAndContinue:
							ErrorInfo err = new ErrorInfo();
							err.mLineNumber = mLineNum;
							err.mExceptionInfo = ex;
//							err.mColumnNumber = mColumnNum;
							err.mRecordString = currentLine;
							mErrorManager.AddError(err);
							break;
					}
				}

			}

			mTotalRecords = records.Length;

            if (mFooterText != null && mFooterText != string.Empty)
                if (mFooterText.EndsWith("\r\n"))
                    writer.Write(mFooterText);
                else
                    writer.WriteLine(mFooterText);

			return true;
		}

		#endregion

		#region "  WriteString  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteString/*'/>
        public string WriteString(T[] records)
		{
			return WriteString(records, -1);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteString2/*'/>
        public string WriteString(T[] records, int maxRecords)
		{
			StringBuilder sb = new StringBuilder();
			StringWriter writer = new StringWriter(sb);
			WriteStream(writer, records, maxRecords);
            string res = writer.ToString();
            writer.Close();
			return res;
		}

		#endregion

		#region "  AppendToFile  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/AppendToFile1/*'/>
        public bool AppendToFile(string fileName, T record)
		{
            return AppendToFile(fileName, new T[] { record });
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/AppendToFile2/*'/>
        public bool AppendToFile(string fileName, T[] records)
		{
			TextWriter writer = StreamHelper.CreateFileAppender(fileName, mEncoding, true, false);

            mHeaderText = String.Empty;
            mFooterText = String.Empty;

            bool res;
			res = WriteStream(writer, records);
			writer.Close();

			return res;
		}

		#endregion
	}
}

#endif