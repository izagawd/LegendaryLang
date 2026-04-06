use Std.Ops.Add;
struct Holder {
    val: (i32 as Add(i32)).Output
}
impl Copy for Holder {}
fn main() -> i32 {
    let h = make Holder { val : 13 };
    h.val
}
