use Std.Core.Ops.Add;
fn main() -> i32 {
    struct Foo {}
    impl Add(Foo) for i32 {
        type Output = i32;
        fn Add(dd: i32, other: Foo) -> i32 {
            dd
        }
    }
    4 + make Foo {}
}
