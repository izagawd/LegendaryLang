struct Holder['a] {
    r: &'a uniq i32
}

enum MaybeHolder['a] {
    Has(Holder['a]),
    Empty
}

fn main() -> i32 {
    let x = 5;
    let m = MaybeHolder.Has(make Holder { r: &uniq x });
    x
}
