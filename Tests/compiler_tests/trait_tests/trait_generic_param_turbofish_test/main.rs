trait Foo {
    fn kk(T:! type) -> i32;
}
fn bruh(T:! Foo, U:! type) -> i32 {
    (T as Foo).kk(U)
}
impl Foo for i32 {
    fn kk(T:! type) -> i32 { 7 }
}
fn main() -> i32 {
    bruh(i32, i32)
}
