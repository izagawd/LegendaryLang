trait Marker {}
trait A : Marker {}

struct Foo {
    val: i32
}

impl A for Foo {}

fn main() -> i32 {
    5
}
