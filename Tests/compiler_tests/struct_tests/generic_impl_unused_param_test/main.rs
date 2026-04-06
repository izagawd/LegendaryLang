struct Bar {
    val: i32
}

trait Foo {
    fn do_thing() -> i32;
}

impl[T:! type] Foo for Bar {
    fn do_thing() -> i32 {
        5
    }
}

fn main() -> i32 {
    (Bar as Foo).do_thing()
}
