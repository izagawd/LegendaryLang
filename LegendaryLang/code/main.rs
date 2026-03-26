trait Foo{
    fn bro() -> i32;
}
impl Foo for i32{
    fn bro() -> i32 {
        3
    }
}
trait Bar{
    fn bar() -> i32;
    }

impl Bar for i32{
    fn bar() -> i32{
        4
        }
    }
fn the_fooer<T: Foo, T: Bar>() -> i32{
    4
}
fn main() -> i32{
    the_fooer::<i32, i32>()
}
