use Std.Core.Ops.Add;
fn main() -> i32 {
    fn inner() -> (i32 as Add(i32)).Output {
        3 + 9
    }
    inner()
}
