struct Yo(T:! Sized){
    dd: T
}
struct Foo{
    boo: i32
}

impl Copy for Foo{}
impl Foo{
    fn make(self: &Self) -> Yo(Foo){
        make Yo {
            dd: *self
        }
    }
}
fn main() -> i32{
    let foo = make Foo {
        boo: 500
    };
    let made: Yo(Foo) = foo.make();
    made.dd.boo
}
