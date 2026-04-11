struct Inner {
    data: GcMut(i32)
}

struct Outer {
    inner: Inner,
    tag: i32
}

fn main() -> i32 {
    {
        let o = make Outer {
            inner: make Inner { data: GcMut.New(999) },
            tag: 7
        };
    }
    42
}
