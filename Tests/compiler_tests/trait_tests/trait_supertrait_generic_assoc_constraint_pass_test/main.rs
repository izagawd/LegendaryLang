use Std.Ops.Add;
trait Addable: Add(i32, Output = i32) {}

impl Addable for i32 {}

fn main() -> i32 {
    42
}
