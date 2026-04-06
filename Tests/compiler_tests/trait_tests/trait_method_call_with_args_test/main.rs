use Std.Core.Ops.Add;
trait Adder {
    fn add_to(self: &Self, other: i32) -> i32;
}
struct Val {
    n: i32
}
impl Adder for Val {
    fn add_to(self: &Val, other: i32) -> i32 {
        self.n + other
    }
}
fn main() -> i32 {
    let v = make Val { n : 10 };
    v.add_to(5)
}
