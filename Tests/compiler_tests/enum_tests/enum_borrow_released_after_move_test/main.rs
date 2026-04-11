struct Holder['a] {
    r: &'a mut i32
}

enum MaybeHolder['a] {
    Has(Holder['a]),
    Empty
}

fn DropNow[T:! type](x: T) {}

fn main() -> i32 {
    let x = 5;
    let m = MaybeHolder.Has(make Holder { r: &mut x });
    DropNow(m);
    x
}
