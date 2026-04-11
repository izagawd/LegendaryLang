use Std.Ops.Drop;

struct Droppable['a] {
    reference: &'a mut i32
}

enum Foo['a] {
    One(Droppable['a]),
    Two
}

impl['a] Drop for Droppable['a] {
    fn Drop(self: &mut Self) {
        *self.reference = *self.reference + 1;
    }
}

fn DropNow[T:! type](input: T) {}

fn main() -> i32 {
    let idk = 0;
    let dd: Foo = Foo.One(make Droppable { reference: &mut idk });
    DropNow(dd);
    idk
}
