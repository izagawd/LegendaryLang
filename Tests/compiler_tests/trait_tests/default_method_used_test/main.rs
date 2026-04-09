trait Greet {
    fn hello() -> i32 {
        42
    }
}

struct Foo {}
impl Copy for Foo {}
impl Greet for Foo {}

fn main() -> i32 {
    (Foo as Greet).hello()
}
