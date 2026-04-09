trait Producer: Sized {
    let Item :! MetaSized;
    fn produce(self: Self) -> &'static (Self as Producer).Item;
}

struct Holder(T:! Producer) {
    val: (T as Producer).Item
}

fn main() -> i32 { 0 }
