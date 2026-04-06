use Std.Ops.Add;
trait Foo {
    fn bro[T:! type](to_add: T) -> i32;
}
impl Foo for i32 {
    fn bro[T:! Add(i32, Output = i32)](to_add: T) -> i32 {
        5
    }
}
fn main() -> i32 {
    0
}
