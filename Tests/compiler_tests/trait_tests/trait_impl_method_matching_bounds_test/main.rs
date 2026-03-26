trait Foo {
    fn bro(x: i32) -> i32;
}
impl Foo for i32 {
    fn bro(x: i32) -> i32 {
        x
    }
}
fn main() -> i32 {
    <i32 as Foo>::bro(5)
}
