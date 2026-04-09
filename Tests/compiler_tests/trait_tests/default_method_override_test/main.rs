trait Greet {
    fn hello() -> i32 {
        42
    }
}

struct Foo {}
impl Copy for Foo {}
impl Greet for Foo {
    fn hello() -> i32 {
        99
    }
}

fn main() -> i32 {
    (Foo as Greet).hello()
}
