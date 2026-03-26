trait Foo {
    fn kk<T>() -> i32;
}
impl Foo for i32 {
    fn kk<T>() -> i32 { 42 }
}
fn main() -> i32 {
    <i32 as Foo>::kk::<i32>()
}
