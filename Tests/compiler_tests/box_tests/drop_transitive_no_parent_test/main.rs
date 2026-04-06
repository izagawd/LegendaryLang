use Std.Core.Marker.Drop;
struct C {
    r: &uniq i32
}

impl Drop for C {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

struct B {
    c: C
}

struct A {
    b: B,
    val: i32
}

fn main() -> i32 {
    let counter = 0;
    {
        let a = make A {
            b: make B {
                c: make C { r: &uniq counter }
            },
            val: 5
        };
    }
    counter
}
