use Std.Core.Ops.Add;
struct Foo {}

impl Add(Foo) for i32 {
    type Output = i32;
    fn Add(dd: i32, other: Foo) -> i32 {
        dd
    }
}

fn main() -> i32 {
    4 + make Foo {}
}
