use Std.Fmt.ToString;
use Std.Fmt.PrintLn;

fn main() -> i32 {
    let s = "World";
    let r: &str = s.ToString();
    PrintLn(&s);
    0
}
