trait Foo {
    fn kk(T:! Sized) -> i32;
}
fn bruh(T:! Sized +Foo, U:! Sized) -> i32 {
    T.kk(U)
}
impl Foo for i32 {
    fn kk(T:! Sized) -> i32 { 13 }
}
fn main() -> i32 {
    bruh(i32, i32)
}
