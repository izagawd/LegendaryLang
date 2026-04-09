trait Foo {
    fn kk(T:! type) -> i32;
}
fn bruh(T:! Foo, U:! type) -> i32 {
    (T as Foo).kk(U);
    T.kk(U)
}
fn main() -> i32 {
    4
}
