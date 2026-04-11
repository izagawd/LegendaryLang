use Std.Marker.Drop;

struct Foo { val: i32 }

impl Drop for Foo {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 { 0 }
