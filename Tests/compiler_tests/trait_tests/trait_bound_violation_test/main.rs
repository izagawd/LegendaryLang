trait Foo{
    fn bro() -> i32;
}
impl Foo for i32{
    fn bro() -> i32 {
        3
    }
}
fn the_fooer(T:! Sized +Foo) -> i32{
    T.bro()
}
fn main() -> i32{
    the_fooer(bool)
}
