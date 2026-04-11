trait Producer: Sized {
    let Item :! type;
    fn produce(self: Self) -> &'static (Self as Producer).Item;
}

struct Holder(T:! Sized +Producer) {
    val: (T as Producer).Item
}

fn main() -> i32 { 0 }
