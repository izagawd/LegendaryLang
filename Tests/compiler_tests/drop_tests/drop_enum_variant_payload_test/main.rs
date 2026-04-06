use Std.Core.Marker.Drop;

struct Droppable['a] {
    reference: &'a uniq i32
}

enum Foo['a] {
    One(Droppable['a]),
    Two
}

impl['a] Drop for Droppable['a] {
    fn Drop(self: &uniq Self) {
        *self.reference = *self.reference + 1;
    }
}

fn DropNow[T:! type](input: T) {}

fn main() -> i32 {
    let idk = 0;
    let dd: Foo = Foo.One(make Droppable { reference: &uniq idk });
    DropNow(dd);
    idk
}
