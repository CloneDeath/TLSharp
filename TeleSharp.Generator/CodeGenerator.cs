namespace TeleSharp.Generator {
	public class CodeGenerator {
		private string _code;
		public int IndentationLevel;
		
		public CodeGenerator(){}
		public CodeGenerator(string existingCode) {
			_code = existingCode;
		}

		public void WriteLine(string line) {
			_code += Indentation + line + NewLine;
		}

		public void WriteLines(params string[] lines) {
			foreach (var line in lines) {
				WriteLine(line);
			}
		}

		public string GetCode() {
			return _code;
		}

		public override string ToString() => GetCode();

		public const char NewLine = '\n';
		protected virtual string Indentation => GetIndentationForLevel(IndentationLevel);

		protected virtual string GetIndentationForLevel(int indentationLevel) {
			return new string(' ', indentationLevel * 4);
		}

		public static CodeGenerator operator +(CodeGenerator self, string line) {
			var newCode = new CodeGenerator(self._code) {
				IndentationLevel = self.IndentationLevel
			};
			newCode.WriteLine(line);
			return newCode;
		}
	}
}